using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EggMapper.Internal;

namespace EggMapper;

public sealed class Mapper : IMapper
{
    private readonly MapperConfiguration _config;
    internal IServiceProvider? ServiceProvider { get; set; }

    [ThreadStatic]
    private static ResolutionContext? _sharedCtx;

    public Mapper(MapperConfiguration config)
    {
        _config = config;
        _generation = config.Generation;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ResolutionContext GetContext(IDictionary<string, object>? items = null)
    {
        var ctx = _sharedCtx ??= new ResolutionContext();
        ctx.Depth = 0;
        ctx.Mapper = this;
        ctx.ServiceProvider = ServiceProvider;
        ctx.Items = items;
        ctx.ClearInstanceCache();
        return ctx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TDestination>(object? source)
    {
        if (source == null) return default!;
        var sourceType = source.GetType();
        var entry = ObjectSourceCache<TDestination>.Entry;
        if (entry != null && entry.SourceType == sourceType && entry.Generation == _generation)
            return (TDestination)entry.Map(source, null, entry.NeedsContext ? GetContext() : null!);
        var previous = ObjectSourceCache<TDestination>.PreviousEntry;
        if (previous != null && previous.SourceType == sourceType && previous.Generation == _generation)
            return (TDestination)previous.Map(source, null, previous.NeedsContext ? GetContext() : null!);
        return MapObjectSourceSlow<TDestination>(source);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TDestination MapObjectSourceSlow<TDestination>(object source)
    {
        var sourceType = source.GetType();
        var destinationType = typeof(TDestination);
        if (TryFindMapDelegate(sourceType, destinationType, out var map, out var needsContext)
            || (!ReflectionHelper.IsCollectionType(destinationType)
                && TryFindSameTypeMap(sourceType, destinationType, out map, out needsContext)))
        {
            ObjectSourceCache<TDestination>.PreviousEntry = ObjectSourceCache<TDestination>.Entry;
            ObjectSourceCache<TDestination>.Entry =
                new ObjectSourceCache<TDestination>.CacheEntry(sourceType, map, needsContext, _generation);
            return (TDestination)map(source, null, needsContext ? GetContext() : null!);
        }
        return (TDestination)MapUnregisteredPair(source, sourceType, destinationType, null, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source != null)
        {
            var entry = FastCache<TSource, TDestination>.Entry;
            if (entry != null && entry.Generation == _generation)
                return entry.Func(source, default!);

            return MapSlow<TSource, TDestination>(source);
        }

        if (typeof(TSource).IsValueType)
        {
            var key = new TypePair(typeof(TSource), typeof(TDestination));
            if (_config.TryGetOrCompileMap(key, out var del))
            {
                var ctx = GetContext();
                return (TDestination)del(source!, null, ctx);
            }
        }
        return default!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TDestination MapSlow<TSource, TDestination>(TSource source)
    {
        var key = new TypePair(typeof(TSource), typeof(TDestination));

        try
        {
            if (_config.TryGetOrCompileCtxFreeMap(key, out var ctxFreeDel))
            {
                var typed = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
                FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
                return typed(source, default!);
            }

            if (_config.TryGetOrCompileMap(key, out var del))
            {
                var ctx = GetContext();
                return (TDestination)del(source!, null, ctx);
            }

            if (_config.TryGetOrCompileOpenGenericMap(key, out var openBoxed, out var openCtxFree))
            {
                if (openCtxFree != null)
                {
                    var typed = (Func<TSource, TDestination, TDestination>)openCtxFree;
                    FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
                    return typed(source, default!);
                }
                var ctx = GetContext();
                return (TDestination)openBoxed!(source!, null, ctx);
            }

            var runtimeType = source!.GetType();
            if (runtimeType != typeof(TSource))
                return (TDestination)MapInternal(source, runtimeType, typeof(TDestination), null);

            if (ReflectionHelper.IsCollectionType(typeof(TDestination)) && source is IEnumerable)
                return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), null);

            if (typeof(TSource).IsValueType && Nullable.GetUnderlyingType(typeof(TSource)) == null)
            {
                var nullableKey = new TypePair(typeof(Nullable<>).MakeGenericType(typeof(TSource)), typeof(TDestination));
                if (_config.TryGetOrCompileMap(nullableKey, out var nullableDel))
                {
                    var ctx = GetContext();
                    return (TDestination)nullableDel(source!, null, ctx);
                }
            }

            if (_config.TryGetOrCompileSameTypeMap(key, out var sameBoxed, out var sameCtxFree))
            {
                if (sameCtxFree != null)
                {
                    var typed = (Func<TSource, TDestination, TDestination>)sameCtxFree;
                    FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
                    return typed(source, default!);
                }
                var ctx = GetContext();
                return (TDestination)sameBoxed!(source!, null, ctx);
            }
        }
        catch (MappingException) { throw; }
        catch (MappingValidationException) { throw; }
        catch (Exception ex)
        {
            throw new MappingException(typeof(TSource), typeof(TDestination), ex);
        }

        throw new InvalidOperationException(
            $"No mapping configured for {TypeNameHelper.Pair(typeof(TSource), typeof(TDestination))}. " +
            $"Call CreateMap<{TypeNameHelper.Readable(typeof(TSource))}, {TypeNameHelper.Readable(typeof(TDestination))}>() in your mapper configuration.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null) return destination;
        var entry = FastCache<TSource, TDestination>.Entry;
        if (entry != null && entry.Generation == _generation)
            return entry.Func(source, destination);
        return MapIntoSlow(source, destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TDestination MapIntoSlow<TSource, TDestination>(TSource source, TDestination destination)
    {
        var key = new TypePair(typeof(TSource), typeof(TDestination));

        if (_config.TryGetOrCompileCtxFreeMap(key, out var ctxFreeDel))
        {
            var typed = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
            FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
            return typed(source, destination);
        }

        if (_config.TryGetOrCompileMap(key, out var del))
        {
            var ctx = GetContext();
            return (TDestination)del(source!, destination, ctx);
        }
        if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out var openCtxFree))
        {
            if (openCtxFree != null)
            {
                var typed = (Func<TSource, TDestination, TDestination>)openCtxFree;
                FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
                return typed(source, destination);
            }
            var ctx = GetContext();
            return (TDestination)openDel!(source!, destination, ctx);
        }

        var runtimeType = source!.GetType();
        if (runtimeType != typeof(TSource))
            return (TDestination)MapInternal(source, runtimeType, typeof(TDestination), destination);

        if (ReflectionHelper.IsCollectionType(typeof(TDestination)) && source is IEnumerable)
            return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), destination);

        if (_config.TryGetOrCompileSameTypeMap(key, out var sameBoxed, out var sameCtxFree))
        {
            if (sameCtxFree != null)
            {
                var typed = (Func<TSource, TDestination, TDestination>)sameCtxFree;
                FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
                return typed(source, destination);
            }
            return (TDestination)sameBoxed!(source, destination, GetContext());
        }

        throw new InvalidOperationException(
            $"No mapping configured for {TypeNameHelper.Pair(typeof(TSource), typeof(TDestination))}. " +
            $"Call CreateMap<{TypeNameHelper.Readable(typeof(TSource))}, {TypeNameHelper.Readable(typeof(TDestination))}>() in your mapper configuration.");
    }

    public object Map(object source, Type sourceType, Type destinationType)
    {
        if (source == null) return null!;
        return MapInternal(source, sourceType, destinationType, null);
    }

    public TDestination Map<TDestination>(object? source, Action<IMappingOperationOptions<object, TDestination>> opts)
    {
        if (source == null) return default!;
        if (opts != null)
        {
            var options = new MappingOperationOptions<object, TDestination>();
            opts(options);
            var dest = (TDestination)MapInternal(source, source.GetType(), typeof(TDestination), null, options.Items);
            options.RunBeforeMapActions(source, dest);
            options.RunAfterMapActions(source, dest);
            return dest;
        }
        return (TDestination)MapInternal(source, source.GetType(), typeof(TDestination), null);
    }

    public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts)
    {
        if (opts != null)
        {
            var options = new MappingOperationOptions<TSource, TDestination>();
            opts(options);
            var mapped = MapWithItems<TSource, TDestination>(source, options.Items);
            options.RunBeforeMapActions(source, mapped);
            options.RunAfterMapActions(source, mapped);
            return mapped;
        }
        return Map<TSource, TDestination>(source);
    }

    private TDestination MapWithItems<TSource, TDestination>(TSource source, IDictionary<string, object>? items)
    {
        if (source == null) return default!;
        var key = new TypePair(typeof(TSource), typeof(TDestination));
        if (_config.TryGetOrCompileMap(key, out var del))
        {
            var ctx = GetContext(items);
            return (TDestination)del(source, null, ctx);
        }
        if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out _))
        {
            var ctx = GetContext(items);
            return (TDestination)openDel!(source, null, ctx);
        }
        return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), null, items);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Patch<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null) return destination;

        var entry = PatchCache<TSource, TDestination>.Entry;
        if (entry != null && entry.Generation == _generation)
            return entry.Func(source, destination);

        return PatchSlow<TSource, TDestination>(source, destination);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private TDestination PatchSlow<TSource, TDestination>(TSource source, TDestination destination)
    {
        var key = new TypePair(typeof(TSource), typeof(TDestination));
        var raw = _config.GetOrCompilePatchDelegate(key);
        if (raw != null)
        {
            var patchDel = (Func<TSource, TDestination, TDestination>)raw;
            PatchCache<TSource, TDestination>.Entry = new PatchCache<TSource, TDestination>.CacheEntry(patchDel, _generation);
            return patchDel(source, destination);
        }

        var runtimeType = source!.GetType();
        if (runtimeType != typeof(TSource))
        {
            for (var bt = runtimeType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
            {
                var baseKey = new TypePair(bt, typeof(TDestination));
                var baseRaw = _config.GetOrCompilePatchDelegate(baseKey);
                if (baseRaw != null)
                {
                    try
                    {
                        return (TDestination)baseRaw.DynamicInvoke(source, destination)!;
                    }
                    catch (System.Reflection.TargetInvocationException tie)
                    {
                        throw new MappingException(runtimeType, typeof(TDestination),
                            tie.InnerException ?? tie);
                    }
                }
            }
        }

        throw new InvalidOperationException(
            $"No mapping configured for {TypeNameHelper.Pair(typeof(TSource), typeof(TDestination))}. " +
            $"Call CreateMap<{TypeNameHelper.Readable(typeof(TSource))}, {TypeNameHelper.Readable(typeof(TDestination))}>() in your mapper configuration.");
    }

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        if (source is List<TSource> directList)
        {
            var listEntry = FastListCache<TSource, TDestination>.Entry;
            if (listEntry != null && listEntry.Generation == _generation)
                return listEntry.Func(directList);

            var itemEntry = FastCache<TSource, TDestination>.Entry;
            if (itemEntry != null && itemEntry.Generation == _generation)
            {
                var count = directList.Count;
                var result = new List<TDestination>(count);
                for (int i = 0; i < count; i++)
                {
                    var item = directList[i];
                    if (item == null) { result.Add(default!); continue; }
                    result.Add(itemEntry.Func(item, default!));
                }
                return result;
            }
        }

        return MapListFallback<TSource, TDestination>(source);
    }

    private List<TDestination> MapListFallback<TSource, TDestination>(IEnumerable<TSource> source)
    {
        var key = new TypePair(typeof(TSource), typeof(TDestination));

        if (source is List<TSource> srcList)
        {
            var listDelRaw = _config.GetOrCompileListDelegate(key);
            if (listDelRaw != null)
            {
                var listDel = (Func<List<TSource>, List<TDestination>>)listDelRaw;
                FastListCache<TSource, TDestination>.Entry = new FastListCache<TSource, TDestination>.CacheEntry(listDel, _generation);

                if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var itemDelRaw))
                {
                    var itemDel = (Func<TSource, TDestination, TDestination>)itemDelRaw;
                    FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(itemDel, _generation);
                }

                return listDel(srcList);
            }
        }

        if (_config.TryGetOrCompileCtxFreeMap(key, out var ctxFreeDel))
        {
            var typedDel = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
            FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typedDel, _generation);
            return MapListWithCtxFreeDelegate(typedDel, source);
        }

        if (!_config.TryGetOrCompileMap(key, out var del))
        {
            if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out var openCtxFree))
            {
                if (openCtxFree != null)
                {
                    var typedDel = (Func<TSource, TDestination, TDestination>)openCtxFree;
                    FastCache<TSource, TDestination>.Entry =
                        new FastCache<TSource, TDestination>.CacheEntry(typedDel, _generation);
                    return MapListWithCtxFreeDelegate(typedDel, source);
                }
                del = openDel!;
            }
            else
            {
                throw new InvalidOperationException(
                    $"No mapping configured for {TypeNameHelper.Pair(typeof(TSource), typeof(TDestination))}.");
            }
        }
        var ctx = GetContext();

        if (source is IList<TSource> list)
        {
            var count = list.Count;
            var resultList = new List<TDestination>(count);
            for (int i = 0; i < count; i++)
            {
                var item = list[i];
                if (item == null) { resultList.Add(default!); continue; }
                ctx.Depth = 0;
                resultList.Add((TDestination)del(item, null, ctx));
            }
            return resultList;
        }

        {
            var resultList = source is ICollection<TSource> col2
                ? new List<TDestination>(col2.Count)
                : new List<TDestination>();
            foreach (var item in source)
            {
                if (item == null) { resultList.Add(default!); continue; }
                ctx.Depth = 0;
                resultList.Add((TDestination)del(item, null, ctx));
            }
            return resultList;
        }
    }

    private static List<TDestination> MapListWithCtxFreeDelegate<TSource, TDestination>(
        Func<TSource, TDestination, TDestination> typedDel,
        IEnumerable<TSource> source)
    {
        if (source is IList<TSource> lst)
        {
            var count = lst.Count;
            var r = new List<TDestination>(count);
            for (int i = 0; i < count; i++)
            {
                var item = lst[i];
                r.Add(item == null ? default! : typedDel(item, default!));
            }
            return r;
        }

        var result = source is ICollection<TSource> col
            ? new List<TDestination>(col.Count)
            : new List<TDestination>();
        foreach (var item in source)
            result.Add(item == null ? default! : typedDel(item, default!));
        return result;
    }

    private object MapInternal(object source, Type sourceType, Type destinationType, object? destination,
        IDictionary<string, object>? items = null)
    {
        if (TryFindMapDelegate(sourceType, destinationType, out var map, out _))
            return map(source, destination, GetContext(items));

        return MapUnregisteredPair(source, sourceType, destinationType, destination, items);
    }

    private object MapUnregisteredPair(object source, Type sourceType, Type destinationType, object? destination,
        IDictionary<string, object>? items)
    {
        if (ReflectionHelper.IsCollectionType(destinationType) && source is IEnumerable srcEnum)
        {
            var destElemType = ReflectionHelper.GetCollectionElementType(destinationType);
            var srcElemType  = ReflectionHelper.GetCollectionElementType(sourceType);
            if (destElemType != null && srcElemType != null)
            {
                var elemDel = FindElementDelegate(srcElemType, destElemType);
                bool identityElements = elemDel == null && destElemType.IsAssignableFrom(srcElemType);

                if (elemDel != null || identityElements)
                {
                    var ctx = GetContext(items);
                    var nullSub = destElemType.IsValueType ? Activator.CreateInstance(destElemType) : null;

                    object MapItem(object item)
                    {
                        if (identityElements) return item;
                        ctx.Depth = 0;
                        return elemDel!(item, null, ctx);
                    }

                    if (destinationType.IsArray && srcEnum is ICollection coll)
                    {
                        var arr = Array.CreateInstance(destElemType, coll.Count);
                        int i = 0;
                        foreach (var item in srcEnum)
                        {
                            arr.SetValue(item == null ? nullSub : MapItem(item), i++);
                        }
                        return arr;
                    }

                    var resultList = (IList)Activator.CreateInstance(
                        typeof(List<>).MakeGenericType(destElemType))!;
                    foreach (var item in srcEnum)
                        resultList.Add(item == null ? nullSub : MapItem(item));

                    if (destinationType.IsAssignableFrom(resultList.GetType()))
                        return resultList;

                    if (destinationType.IsArray)
                    {
                        var arr = Array.CreateInstance(destElemType, resultList.Count);
                        resultList.CopyTo(arr, 0);
                        return arr;
                    }

                    if (destinationType.IsGenericType && !destinationType.IsAbstract)
                    {
                        var emptyCtor = destinationType.GetConstructor(Type.EmptyTypes);
                        var addMethod = destinationType.GetMethod("Add", new[] { destElemType });
                        if (emptyCtor != null && addMethod != null)
                        {
                            var dest = emptyCtor.Invoke(Array.Empty<object>());
                            var addArgs = new object?[1];
                            for (int i = 0; i < resultList.Count; i++)
                            {
                                addArgs[0] = resultList[i];
                                addMethod.Invoke(dest, addArgs);
                            }
                            return dest!;
                        }
                    }

                    var wrapperCtor = Execution.ExpressionBuilder.FindCollectionWrapperCtor(
                        destinationType, resultList.GetType());
                    if (wrapperCtor != null)
                    {
                        try { return wrapperCtor.Invoke(new object[] { resultList }); }
                        catch (System.Reflection.TargetInvocationException tie) when (tie.InnerException != null)
                        {
                            throw tie.InnerException;
                        }
                    }

                    throw new InvalidOperationException(
                        $"Cannot materialize collection of type '{TypeNameHelper.Readable(destinationType)}' " +
                        $"from List<{TypeNameHelper.Readable(destElemType)}>: " +
                        $"destination is not assignable from List<T>, not an array, and has no " +
                        $"single-argument constructor accepting List<T> or any of its interfaces.");
                }

                throw new InvalidOperationException(
                    $"No mapping configured for collection {TypeNameHelper.Pair(sourceType, destinationType)}. " +
                    $"Register the element mapping: CreateMap<{TypeNameHelper.Readable(srcElemType)}, {TypeNameHelper.Readable(destElemType)}>().");
            }
        }

        var sameKey = new TypePair(sourceType, destinationType);
        if (_config.TryGetOrCompileSameTypeMap(sameKey, out var sameBoxed, out _))
        {
            var ctx = GetContext(items);
            return sameBoxed!(source, destination, ctx);
        }

        throw new InvalidOperationException(
            $"No mapping configured for {TypeNameHelper.Pair(sourceType, destinationType)}. " +
            $"Call CreateMap<{TypeNameHelper.Readable(sourceType)}, {TypeNameHelper.Readable(destinationType)}>() in your mapper configuration.");
    }

    internal Func<TSource, TDestination, TDestination>? GetCachedCtxFreeMap<TSource, TDestination>()
    {
        var entry = FastCache<TSource, TDestination>.Entry;
        return entry != null && entry.Generation == _generation ? entry.Func : null;
    }

    private bool TryFindMapDelegate(Type sourceType, Type destinationType,
        out Func<object, object?, ResolutionContext, object> map, out bool needsContext)
    {
        var key = new TypePair(sourceType, destinationType);
        if (TryGetConfiguredMap(key, out map, out needsContext))
            return true;

        if (sourceType.IsValueType && Nullable.GetUnderlyingType(sourceType) == null
            && TryGetConfiguredMap(new TypePair(typeof(Nullable<>).MakeGenericType(sourceType), destinationType),
                out map, out needsContext))
            return true;

        if (_config.TryGetOrCompileOpenGenericMap(key, out var openMap, out var openCtxFree))
        {
            map = openMap!;
            needsContext = openCtxFree == null;
            return true;
        }

        for (var baseType = sourceType.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
        {
            if (TryGetConfiguredMap(new TypePair(baseType, destinationType), out map, out needsContext))
                return true;
        }

        foreach (var iface in sourceType.GetInterfaces())
        {
            if (TryGetConfiguredMap(new TypePair(iface, destinationType), out map, out needsContext))
                return true;
        }

        map = null!;
        needsContext = false;
        return false;
    }

    private bool TryGetConfiguredMap(TypePair key,
        out Func<object, object?, ResolutionContext, object> map, out bool needsContext)
    {
        if (_config.TryGetOrCompileMap(key, out var found))
        {
            map = found;
            needsContext = !_config.FrozenCtxFreeMaps.ContainsKey(key);
            return true;
        }
        map = null!;
        needsContext = false;
        return false;
    }

    private bool TryFindSameTypeMap(Type sourceType, Type destinationType,
        out Func<object, object?, ResolutionContext, object> map, out bool needsContext)
    {
        if (_config.TryGetOrCompileSameTypeMap(new TypePair(sourceType, destinationType), out var sameMap, out var sameCtxFree))
        {
            map = sameMap!;
            needsContext = sameCtxFree == null;
            return true;
        }
        map = null!;
        needsContext = false;
        return false;
    }

    private Func<object, object?, ResolutionContext, object>? FindElementDelegate(Type srcElemType, Type destElemType)
    {
        var elemKey = new TypePair(srcElemType, destElemType);

        if (_config.TryGetOrCompileMap(elemKey, out var elemDel))
            return elemDel;

        if (_config.TryGetOrCompileOpenGenericMap(elemKey, out var openDel, out _))
            return openDel;

        for (var bt = srcElemType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
        {
            var bk = new TypePair(bt, destElemType);
            if (_config.TryGetOrCompileMap(bk, out elemDel))
                return elemDel;
            if (_config.TryGetOrCompileOpenGenericMap(bk, out openDel, out _))
                return openDel;
        }

        foreach (var iface in srcElemType.GetInterfaces())
        {
            var ik = new TypePair(iface, destElemType);
            if (_config.TryGetOrCompileMap(ik, out elemDel))
                return elemDel;
            if (_config.TryGetOrCompileOpenGenericMap(ik, out openDel, out _))
                return openDel;
        }

        if (srcElemType.IsValueType && Nullable.GetUnderlyingType(srcElemType) == null)
        {
            var nullableKey = new TypePair(typeof(Nullable<>).MakeGenericType(srcElemType), destElemType);
            if (_config.TryGetOrCompileMap(nullableKey, out elemDel))
                return elemDel;
            if (_config.TryGetOrCompileOpenGenericMap(nullableKey, out openDel, out _))
                return openDel;
        }

        if (_config.TryGetOrCompileSameTypeMap(elemKey, out var sameDel, out _))
            return sameDel;

        return null;
    }

    private readonly int _generation;

    private static class FastCache<TSource, TDestination>
    {
        public static volatile CacheEntry? Entry;
        internal sealed class CacheEntry(Func<TSource, TDestination, TDestination> func, int generation)
        {
            public readonly Func<TSource, TDestination, TDestination> Func = func;
            public readonly int Generation = generation;
        }
    }

    private static class ObjectSourceCache<TDestination>
    {
        public static volatile CacheEntry? Entry;
        public static volatile CacheEntry? PreviousEntry;
        internal sealed class CacheEntry(
            Type sourceType,
            Func<object, object?, ResolutionContext, object> map,
            bool needsContext,
            int generation)
        {
            public readonly Type SourceType = sourceType;
            public readonly Func<object, object?, ResolutionContext, object> Map = map;
            public readonly bool NeedsContext = needsContext;
            public readonly int Generation = generation;
        }
    }

    private static class FastListCache<TSource, TDestination>
    {
        public static volatile CacheEntry? Entry;
        internal sealed class CacheEntry(Func<List<TSource>, List<TDestination>> func, int generation)
        {
            public readonly Func<List<TSource>, List<TDestination>> Func = func;
            public readonly int Generation = generation;
        }
    }

    private static class PatchCache<TSource, TDestination>
    {
        public static volatile CacheEntry? Entry;
        internal sealed class CacheEntry(Func<TSource, TDestination, TDestination> func, int generation)
        {
            public readonly Func<TSource, TDestination, TDestination> Func = func;
            public readonly int Generation = generation;
        }
    }
}

internal sealed class MappingOperationOptions<TSource, TDestination> : IMappingOperationOptions<TSource, TDestination>
{
    private List<Action<TSource, TDestination>>? _beforeActions;
    private List<Action<TSource, TDestination>>? _afterActions;

    public IDictionary<string, object> Items { get; } = new Dictionary<string, object>();

    public void BeforeMap(Action<TSource, TDestination> beforeFunction)
        => (_beforeActions ??= new List<Action<TSource, TDestination>>()).Add(beforeFunction);

    public void AfterMap(Action<TSource, TDestination> afterFunction)
        => (_afterActions ??= new List<Action<TSource, TDestination>>()).Add(afterFunction);

    internal void RunBeforeMapActions(TSource source, TDestination destination)
    {
        if (_beforeActions == null) return;
        for (int i = 0; i < _beforeActions.Count; i++)
            _beforeActions[i](source, destination);
    }

    internal void RunAfterMapActions(TSource source, TDestination destination)
    {
        if (_afterActions == null) return;
        for (int i = 0; i < _afterActions.Count; i++)
            _afterActions[i](source, destination);
    }
}

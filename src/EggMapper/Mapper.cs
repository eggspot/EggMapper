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
        return (TDestination)MapInternal(source, source.GetType(), typeof(TDestination), null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source != null)
        {
            // Ultra-fast path: single volatile field read + null check.
            // Entry bundles Func+Generation atomically — no torn reads between them.
            var entry = FastCache<TSource, TDestination>.Entry;
            if (entry != null && entry.Generation == _generation)
                return entry.Func(source, default!);

            return MapSlow<TSource, TDestination>(source);
        }

        // Null source: check for value type ConvertUsing
        if (typeof(TSource).IsValueType)
        {
            var key = new TypePair(typeof(TSource), typeof(TDestination));
            if (_config.FrozenMaps.TryGetValue(key, out var del))
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
            // Try ctx-free typed delegate
            if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
            {
                var typed = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
                FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typed, _generation);
                return typed(source, default!);
            }

            // Fallback: ctx-aware boxed delegate
            if (_config.FrozenMaps.TryGetValue(key, out var del))
            {
                var ctx = GetContext();
                return (TDestination)del(source!, null, ctx);
            }

            // Open generic on-demand compilation
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

            // Runtime-type fallback: EF Core proxies have a runtime type different from TSource.
            // Delegate to MapInternal which does a base-type walk + collection auto-mapping.
            var runtimeType = source!.GetType();
            if (runtimeType != typeof(TSource))
                return (TDestination)MapInternal(source, runtimeType, typeof(TDestination), null);

            // Collection auto-mapping: Map<List<A>, List<B>>(list) uses registered element map A→B.
            // No need for explicit CreateMap<List<A>, List<B>>().
            if (ReflectionHelper.IsCollectionType(typeof(TDestination)) && source is IEnumerable)
                return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), null);

            // Same-type auto-mapping: T → T without explicit CreateMap<T,T>().
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
        var key = new TypePair(typeof(TSource), typeof(TDestination));

        // Ctx-free typed delegate: zero boxing, zero ResolutionContext allocation.
        // The Func<TSrc, TDest, TDest> signature already accepts an existing destination.
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
            return ((Func<TSource, TDestination, TDestination>)ctxFreeDel)(source, destination);

        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = GetContext();
            return (TDestination)del(source, destination, ctx);
        }
        if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out var openCtxFree))
        {
            if (openCtxFree != null)
                return ((Func<TSource, TDestination, TDestination>)openCtxFree)(source, destination);
            var ctx = GetContext();
            return (TDestination)openDel!(source, destination, ctx);
        }

        // Runtime-type fallback: EF Core proxies have a runtime type different from TSource.
        var runtimeType = source!.GetType();
        if (runtimeType != typeof(TSource))
            return (TDestination)MapInternal(source, runtimeType, typeof(TDestination), destination);

        // Collection auto-mapping fallback: delegates to MapInternal which detects collection types.
        if (ReflectionHelper.IsCollectionType(typeof(TDestination)) && source is IEnumerable)
            return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), destination);

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
        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = GetContext(items);
            return (TDestination)del(source, null, ctx);
        }
        if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out _))
        {
            var ctx = GetContext(items);
            return (TDestination)openDel!(source, null, ctx);
        }
        // Fallback to MapInternal which handles base-type walk, interface walk,
        // and collection auto-mapping — preserving the items dictionary.
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

        // Runtime-type fallback: walk base types for proxy/derived types.
        var runtimeType = source!.GetType();
        if (runtimeType != typeof(TSource))
        {
            for (var bt = runtimeType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
            {
                var baseKey = new TypePair(bt, typeof(TDestination));
                var baseRaw = _config.GetOrCompilePatchDelegate(baseKey);
                if (baseRaw != null)
                {
                    // Invoke via dynamic delegate — the patch func is Func<TBase,TDest,TDest>
                    // but source is a derived type, so it's assignable.
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

        // Ultra-fast path: inlined-loop list delegate (entire collection in one compiled call)
        if (source is List<TSource> directList)
        {
            var listEntry = FastListCache<TSource, TDestination>.Entry;
            if (listEntry != null && listEntry.Generation == _generation)
                return listEntry.Func(directList);

            // Per-item FastCache fallback for List<T>
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

        // Ctx-free list delegate — entire loop inlined; also populate FastListCache for next call
        if (source is List<TSource> srcList)
        {
            var listDelRaw = _config.GetOrCompileListDelegate(key);
            if (listDelRaw != null)
            {
                var listDel = (Func<List<TSource>, List<TDestination>>)listDelRaw;
                FastListCache<TSource, TDestination>.Entry = new FastListCache<TSource, TDestination>.CacheEntry(listDel, _generation);

                // Also warm the per-item FastCache so MapList<> next call uses the fast path
                if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var itemDelRaw))
                {
                    var itemDel = (Func<TSource, TDestination, TDestination>)itemDelRaw;
                    FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(itemDel, _generation);
                }

                return listDel(srcList);
            }
        }

        // Ctx-free per-item delegate — also populate FastCache for next call
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
        {
            var typedDel = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
            FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typedDel, _generation);
            return MapListWithCtxFreeDelegate(typedDel, source);
        }

        // Ctx-aware boxed delegate — or open generic on-demand compilation
        if (!_config.FrozenMaps.TryGetValue(key, out var del))
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
        var key = new TypePair(sourceType, destinationType);
        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = GetContext(items);
            return del(source, destination, ctx);
        }

        // Nullable<T> boxing: .NET boxes Nullable<T> as T, stripping the wrapper.
        // If CreateMap<T?, U>() was registered, the key uses Nullable<T> but source.GetType() returns T.
        // Try Nullable<sourceType> as fallback.
        if (sourceType.IsValueType)
        {
            var nullableKey = new TypePair(typeof(Nullable<>).MakeGenericType(sourceType), destinationType);
            if (_config.FrozenMaps.TryGetValue(nullableKey, out del))
            {
                var ctx = GetContext(items);
                return del(source, destination, ctx);
            }
        }

        // Open generic on-demand compilation
        if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out _))
        {
            var ctx = GetContext(items);
            return openDel!(source, destination, ctx);
        }

        // Base-type walk: resolve EF Core proxy / derived types to their registered base mapping.
        for (var baseType = sourceType.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
        {
            var baseKey = new TypePair(baseType, destinationType);
            if (_config.FrozenMaps.TryGetValue(baseKey, out var baseDel))
            {
                var ctx = GetContext(items);
                return baseDel(source, destination, ctx);
            }
        }

        // Interface walk: resolve interface-based mappings (e.g., CreateMap<IMyEntity, MyDto>()).
        // Class hierarchy walk above only covers concrete base classes — interfaces need separate check.
        foreach (var iface in sourceType.GetInterfaces())
        {
            var ifaceKey = new TypePair(iface, destinationType);
            if (_config.FrozenMaps.TryGetValue(ifaceKey, out var ifaceDel))
            {
                var ctx = GetContext(items);
                return ifaceDel(source, destination, ctx);
            }
        }

        // Collection auto-mapping: Map<IList<T>>(List<S>) → List<T> using registered element map S→T.
        // CreateMap<A,B>() automatically enables Map<List<B>>(listOfA) — no explicit collection map needed.
        // Supports List<>, IList<>, ICollection<>, IEnumerable<>, arrays as source/dest.
        if (ReflectionHelper.IsCollectionType(destinationType) && source is IEnumerable srcEnum)
        {
            var destElemType = ReflectionHelper.GetCollectionElementType(destinationType);
            var srcElemType  = ReflectionHelper.GetCollectionElementType(sourceType);
            if (destElemType != null && srcElemType != null)
            {
                var elemDel = FindElementDelegate(srcElemType, destElemType);
                if (elemDel != null)
                {
                    var resultList = (IList)Activator.CreateInstance(
                        typeof(List<>).MakeGenericType(destElemType))!;
                    var ctx = GetContext(items);
                    foreach (var item in srcEnum)
                    {
                        if (item == null) { resultList.Add(null); continue; }
                        ctx.Depth = 0;
                        resultList.Add(elemDel(item, null, ctx));
                    }
                    return resultList;
                }

                // Element mapping not found — give a clear error pointing to the element types
                throw new InvalidOperationException(
                    $"No mapping configured for collection {TypeNameHelper.Pair(sourceType, destinationType)}. " +
                    $"Register the element mapping: CreateMap<{TypeNameHelper.Readable(srcElemType)}, {TypeNameHelper.Readable(destElemType)}>().");
            }
        }

        // Same-type auto-mapping: T → T without explicit CreateMap<T,T>().
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

    /// <summary>
    /// Finds a boxed element mapping delegate for srcElemType→destElemType.
    /// Checks FrozenMaps, then boxed wrappers of FrozenCtxFreeMaps, then open generics,
    /// then base-type walk and interface walk (for EF Core proxies).
    /// </summary>
    private Func<object, object?, ResolutionContext, object>? FindElementDelegate(Type srcElemType, Type destElemType)
    {
        var elemKey = new TypePair(srcElemType, destElemType);

        // 1. Direct lookup in FrozenMaps (covers both ctx-aware and boxed ctx-free delegates)
        if (_config.FrozenMaps.TryGetValue(elemKey, out var elemDel))
            return elemDel;

        // 2. Open generic on-demand compilation (e.g., CreateMap(typeof(A<>), typeof(B<>)))
        if (_config.TryGetOrCompileOpenGenericMap(elemKey, out var openDel, out _))
            return openDel;

        // 3. Base-type walk for EF Core proxy / derived element types
        for (var bt = srcElemType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
        {
            var bk = new TypePair(bt, destElemType);
            if (_config.FrozenMaps.TryGetValue(bk, out elemDel))
                return elemDel;
            if (_config.TryGetOrCompileOpenGenericMap(bk, out openDel, out _))
                return openDel;
        }

        // 4. Interface walk for interface-based element mappings
        foreach (var iface in srcElemType.GetInterfaces())
        {
            var ik = new TypePair(iface, destElemType);
            if (_config.FrozenMaps.TryGetValue(ik, out elemDel))
                return elemDel;
            if (_config.TryGetOrCompileOpenGenericMap(ik, out openDel, out _))
                return openDel;
        }

        // 5. Same-type auto-mapping for collection elements
        if (_config.TryGetOrCompileSameTypeMap(elemKey, out var sameDel, out _))
            return sameDel;

        return null;
    }

    // Generation is tied to MapperConfiguration, not Mapper instances.
    // All scoped Mapper instances from the same config share a generation,
    // so FastCache stays valid across request scopes.
    private readonly int _generation;

    /// <summary>
    /// Lock-free single-slot global cache for Map&lt;S,D&gt;. Zero overhead after first call.
    /// JIT specializes each (TSource,TDestination) pair into a direct static field read.
    /// Uses a single volatile reference so Func+Generation are always read/written atomically.
    /// </summary>
    private static class FastCache<TSource, TDestination>
    {
        public static volatile CacheEntry? Entry;
        internal sealed class CacheEntry(Func<TSource, TDestination, TDestination> func, int generation)
        {
            public readonly Func<TSource, TDestination, TDestination> Func = func;
            public readonly int Generation = generation;
        }
    }

    /// <summary>
    /// Lock-free single-slot global cache for MapList. Zero overhead after first call.
    /// Uses a single volatile reference so Func+Generation are always read/written atomically.
    /// </summary>
    private static class FastListCache<TSource, TDestination>
    {
        public static volatile CacheEntry? Entry;
        internal sealed class CacheEntry(Func<List<TSource>, List<TDestination>> func, int generation)
        {
            public readonly Func<List<TSource>, List<TDestination>> Func = func;
            public readonly int Generation = generation;
        }
    }

    /// <summary>
    /// Lock-free single-slot global cache for Patch&lt;S,D&gt;. Zero overhead after first call.
    /// Uses a single volatile reference so Func+Generation are always read/written atomically.
    /// </summary>
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

/// <summary>
/// Collects BeforeMap/AfterMap callbacks registered via the call-site opts delegate and
/// runs them after the core mapping completes.
/// </summary>
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

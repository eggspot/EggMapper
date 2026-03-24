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
    private ResolutionContext GetContext()
    {
        var ctx = _sharedCtx ??= new ResolutionContext();
        ctx.Depth = 0;
        ctx.Mapper = this;
        ctx.ServiceProvider = ServiceProvider;
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
            return (TDestination)del(source, null, ctx);
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
            return (TDestination)openBoxed!(source, null, ctx);
        }

        // Runtime-type fallback: EF Core proxies have a runtime type different from TSource.
        // Delegate to MapInternal which does a base-type walk.
        var runtimeType = source!.GetType();
        if (runtimeType != typeof(TSource))
            return (TDestination)MapInternal(source, runtimeType, typeof(TDestination), null);

        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
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
        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
    }

    public object Map(object source, Type sourceType, Type destinationType)
        => MapInternal(source, sourceType, destinationType, null);

    public TDestination Map<TDestination>(object? source, Action<IMappingOperationOptions<object, TDestination>> opts)
    {
        if (source == null) return default!;
        var mapped = (TDestination)MapInternal(source, source.GetType(), typeof(TDestination), null);
        if (opts != null)
        {
            var options = new MappingOperationOptions<object, TDestination>();
            opts(options);
            options.RunBeforeMapActions(source, mapped);
            options.RunAfterMapActions(source, mapped);
        }
        return mapped;
    }

    public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts)
    {
        var mapped = Map<TSource, TDestination>(source);
        if (opts != null)
        {
            var options = new MappingOperationOptions<TSource, TDestination>();
            opts(options);
            options.RunBeforeMapActions(source, mapped);
            options.RunAfterMapActions(source, mapped);
        }
        return mapped;
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
        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
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
                    result.Add(itemEntry.Func(directList[i], default!));
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
                    $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}.");
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

    private object MapInternal(object source, Type sourceType, Type destinationType, object? destination)
    {
        var key = new TypePair(sourceType, destinationType);
        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = GetContext();
            return del(source, destination, ctx);
        }

        // Open generic on-demand compilation
        if (_config.TryGetOrCompileOpenGenericMap(key, out var openDel, out _))
        {
            var ctx = GetContext();
            return openDel!(source, destination, ctx);
        }

        // Base-type walk: resolve EF Core proxy / derived types to their registered base mapping.
        for (var baseType = sourceType.BaseType; baseType != null && baseType != typeof(object); baseType = baseType.BaseType)
        {
            var baseKey = new TypePair(baseType, destinationType);
            if (_config.FrozenMaps.TryGetValue(baseKey, out var baseDel))
            {
                var ctx = GetContext();
                return baseDel(source, destination, ctx);
            }
        }

        // Collection auto-mapping: Map<IList<T>>(List<S>) → List<T> using registered element map S→T.
        // Supports IList<T>, ICollection<T>, IEnumerable<T>, List<T> as destination types.
        if (ReflectionHelper.IsCollectionType(destinationType) && source is IEnumerable srcEnum)
        {
            var destElemType = ReflectionHelper.GetCollectionElementType(destinationType);
            var srcElemType  = ReflectionHelper.GetCollectionElementType(sourceType);
            if (destElemType != null && srcElemType != null)
            {
                var elemKey = new TypePair(srcElemType, destElemType);
                if (!_config.FrozenMaps.TryGetValue(elemKey, out var elemDel))
                {
                    // Walk base types for collection element proxy/derived types
                    for (var bt = srcElemType.BaseType; bt != null && bt != typeof(object); bt = bt.BaseType)
                    {
                        var bk = new TypePair(bt, destElemType);
                        if (_config.FrozenMaps.TryGetValue(bk, out elemDel))
                            break;
                    }
                }
                if (elemDel != null)
                {
                    var resultList = (IList)Activator.CreateInstance(
                        typeof(List<>).MakeGenericType(destElemType))!;
                    var ctx = GetContext();
                    foreach (var item in srcEnum)
                    {
                        if (item == null) { resultList.Add(null); continue; }
                        ctx.Depth = 0;
                        resultList.Add(elemDel(item, null, ctx));
                    }
                    return resultList;
                }
            }
        }

        throw new InvalidOperationException(
            $"No mapping configured for {sourceType.Name} -> {destinationType.Name}. " +
            $"Call CreateMap<{sourceType.Name}, {destinationType.Name}>() in your mapper configuration.");
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

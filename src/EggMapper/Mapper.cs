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
        _generation = System.Threading.Interlocked.Increment(ref _globalGeneration);
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
    public TDestination Map<TDestination>(object source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
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

        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null) return destination;
        var key = new TypePair(typeof(TSource), typeof(TDestination));
        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = GetContext();
            return (TDestination)del(source, destination, ctx);
        }
        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
    }

    public object Map(object source, Type sourceType, Type destinationType)
        => MapInternal(source, sourceType, destinationType, null);

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
        if (_config.FrozenPatchMaps.TryGetValue(key, out var raw))
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
        if (source is List<TSource> srcList
            && _config.FrozenCtxFreeListMaps.TryGetValue(key, out var listDelRaw))
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

        // Ctx-free per-item delegate — also populate FastCache for next call
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
        {
            var typedDel = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
            FastCache<TSource, TDestination>.Entry = new FastCache<TSource, TDestination>.CacheEntry(typedDel, _generation);

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

        // Ctx-aware boxed delegate
        if (!_config.FrozenMaps.TryGetValue(key, out var del))
            throw new InvalidOperationException(
                $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}.");
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

    private object MapInternal(object source, Type sourceType, Type destinationType, object? destination)
    {
        var key = new TypePair(sourceType, destinationType);
        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = GetContext();
            return del(source, destination, ctx);
        }
        throw new InvalidOperationException(
            $"No mapping configured for {sourceType.Name} -> {destinationType.Name}. " +
            $"Call CreateMap<{sourceType.Name}, {destinationType.Name}>() in your mapper configuration.");
    }

    /// <summary>
    // Global generation counter — incremented every time a new Mapper is created.
    // Cached delegates are only valid for the current generation.
    private static int _globalGeneration;
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

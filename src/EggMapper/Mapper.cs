using System.Collections.Generic;
using System.Runtime.CompilerServices;
using EggMapper.Internal;

namespace EggMapper;

public sealed class Mapper : IMapper
{
    private readonly MapperConfiguration _config;

    // Thread-local ResolutionContext pool: avoids a heap allocation on every
    // Map call.  The context is reset (Depth = 0) before each top-level call so
    // nested delegates always start at depth zero.
    [ThreadStatic]
    private static ResolutionContext? _sharedCtx;

    public Mapper(MapperConfiguration config) => _config = config;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TDestination>(object source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        return (TDestination)MapInternal(source, source.GetType(), typeof(TDestination), null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source == null) return default!;

        // Fast path: check per-mapper generic cache first (no dictionary lookup)
        var cached = TypePairCache<TSource, TDestination>.GetCached(this);
        if (cached != null)
            return cached(source);

        var key = new TypePair(typeof(TSource), typeof(TDestination));

        // Ctx-free typed delegate — zero boxing, zero ctx overhead
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
        {
            var typed = (Func<TSource, TDestination>)ctxFreeDel;
            TypePairCache<TSource, TDestination>.SetCached(this, typed);
            return typed(source);
        }

        // Fallback: ctx-aware boxed delegate
        if (_config.FrozenMaps.TryGetValue(key, out var del))
        {
            var ctx = _sharedCtx ??= new ResolutionContext();
            ctx.Depth = 0;
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
            var ctx = _sharedCtx ??= new ResolutionContext();
            ctx.Depth = 0;
            return (TDestination)del(source, destination, ctx);
        }
        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
    }

    public object Map(object source, Type sourceType, Type destinationType)
        => MapInternal(source, sourceType, destinationType, null);

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // Ultra-fast path: check per-mapper list cache first (zero dict lookups after warm-up)
        if (source is IList<TSource> ilist)
        {
            var cachedListDel = ListCache<TSource, TDestination>.GetCached(this);
            if (cachedListDel != null)
                return cachedListDel(ilist);
        }

        var key = new TypePair(typeof(TSource), typeof(TDestination));

        // Ctx-free list delegate: entire collection mapping compiled as single expression tree
        if (source is IList<TSource> lst2 && _config.FrozenCtxFreeListMaps.TryGetValue(key, out var listDel))
        {
            var typedListDel = (Func<IList<TSource>, List<TDestination>>)listDel;
            ListCache<TSource, TDestination>.SetCached(this, typedListDel);
            return typedListDel(lst2);
        }

        // Ctx-free element delegate: per-element typed delegate
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
        {
            var typedDel = (Func<TSource, TDestination>)ctxFreeDel;

            if (source is IList<TSource> lst)
            {
                var count = lst.Count;
                var r = new List<TDestination>(count);
                for (int i = 0; i < count; i++)
                {
                    var item = lst[i];
                    r.Add(item == null ? default! : typedDel(item));
                }
                return r;
            }

            var result = source is ICollection<TSource> col
                ? new List<TDestination>(col.Count)
                : new List<TDestination>();
            foreach (var item in source)
                result.Add(item == null ? default! : typedDel(item));
            return result;
        }

        // Fallback: ctx-aware boxed delegate
        if (!_config.FrozenMaps.TryGetValue(key, out var del))
            throw new InvalidOperationException(
                $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
                $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
        var ctx = _sharedCtx ??= new ResolutionContext();
        ctx.Depth = 0;

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
            var ctx = _sharedCtx ??= new ResolutionContext();
            ctx.Depth = 0;
            return del(source, destination, ctx);
        }
        throw new InvalidOperationException(
            $"No mapping configured for {sourceType.Name} -> {destinationType.Name}. " +
            $"Call CreateMap<{sourceType.Name}, {destinationType.Name}>() in your mapper configuration.");
    }

    /// <summary>
    /// Static generic cache for MapList delegates — eliminates dictionary lookups.
    /// </summary>
    private static class ListCache<TSource, TDestination>
    {
        private static Mapper? _cachedMapper;
        private static Func<IList<TSource>, List<TDestination>>? _cachedDelegate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<IList<TSource>, List<TDestination>>? GetCached(Mapper mapper)
        {
            if (ReferenceEquals(_cachedMapper, mapper))
                return _cachedDelegate;
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCached(Mapper mapper, Func<IList<TSource>, List<TDestination>> del)
        {
            _cachedDelegate = del;
            _cachedMapper = mapper;
        }
    }

    /// <summary>
    /// Static generic cache that eliminates dictionary lookups for the most common
    /// Map&lt;TSource, TDestination&gt; calls. Each unique (TSource, TDestination)
    /// pair gets its own JIT-specialized static field.
    /// </summary>
    private static class TypePairCache<TSource, TDestination>
    {
        // Single-slot cache: stores the last Mapper instance and its typed delegate.
        // For single-mapper applications (the common case), this eliminates ALL
        // dictionary lookups after the first call.
        private static Mapper? _cachedMapper;
        private static Func<TSource, TDestination>? _cachedDelegate;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Func<TSource, TDestination>? GetCached(Mapper mapper)
        {
            // Volatile read of mapper reference; if it matches, the delegate is valid.
            if (ReferenceEquals(_cachedMapper, mapper))
                return _cachedDelegate;
            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetCached(Mapper mapper, Func<TSource, TDestination> del)
        {
            _cachedDelegate = del;
            _cachedMapper = mapper;
        }
    }
}

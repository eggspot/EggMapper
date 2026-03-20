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
            // Ultra-fast path: single static field read + null check.
            // Generation is embedded in the func — if mapper changed, func is null.
            var fast = FastCache<TSource, TDestination>.Func;
            if (fast != null & FastCache<TSource, TDestination>.Generation == _generation)
                return fast(source, default!);

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
            FastCache<TSource, TDestination>.Func = typed;
            FastCache<TSource, TDestination>.Generation = _generation;
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

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        // Ultra-fast path: inlined-loop list delegate (entire collection in one compiled call)
        if (source is List<TSource> directList)
        {
            var listFunc = FastListCache<TSource, TDestination>.Func;
            if (listFunc != null & FastListCache<TSource, TDestination>.Generation == _generation)
                return listFunc(directList);

            // Per-item FastCache fallback for List<T>
            var itemFunc = FastCache<TSource, TDestination>.Func;
            if (itemFunc != null & FastCache<TSource, TDestination>.Generation == _generation)
            {
                var count = directList.Count;
                var result = new List<TDestination>(count);
                for (int i = 0; i < count; i++)
                    result.Add(itemFunc(directList[i], default!));
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
            FastListCache<TSource, TDestination>.Func = listDel;
            FastListCache<TSource, TDestination>.Generation = _generation;

            // Also warm the per-item FastCache so MapList<> next call uses the fast path
            if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeRaw))
            {
                FastCache<TSource, TDestination>.Func = (Func<TSource, TDestination, TDestination>)ctxFreeRaw;
                FastCache<TSource, TDestination>.Generation = _generation;
            }

            return listDel(srcList);
        }

        // Ctx-free per-item delegate — also populate FastCache for next call
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
        {
            var typedDel = (Func<TSource, TDestination, TDestination>)ctxFreeDel;
            FastCache<TSource, TDestination>.Func = typedDel;
            FastCache<TSource, TDestination>.Generation = _generation;

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
    /// Stores Func&lt;TSrc,TDest,TDest&gt; — the compiled ctx-free delegate — to avoid the
    /// extra closure call that a NoDestWrapper would add.
    /// </summary>
    private static class FastCache<TSource, TDestination>
    {
        public static Func<TSource, TDestination, TDestination>? Func;
        public static int Generation;
    }

    /// <summary>
    /// Lock-free single-slot global cache for MapList. Zero overhead after first call.
    /// </summary>
    private static class FastListCache<TSource, TDestination>
    {
        public static Func<List<TSource>, List<TDestination>>? Func;
        public static int Generation;
    }
}

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
        var del = GetDelegate<TSource, TDestination>();
        var ctx = _sharedCtx ??= new ResolutionContext();
        ctx.Depth = 0;
        return (TDestination)del(source, null, ctx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null) return destination;
        var del = GetDelegate<TSource, TDestination>();
        var ctx = _sharedCtx ??= new ResolutionContext();
        ctx.Depth = 0;
        return (TDestination)del(source, destination, ctx);
    }

    public object Map(object source, Type sourceType, Type destinationType)
        => MapInternal(source, sourceType, destinationType, null);

    public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var key = new TypePair(typeof(TSource), typeof(TDestination));

        // Ctx-free fast path: Func<TSource,TDestination> — zero boxing, no ctx overhead
        if (_config.FrozenCtxFreeMaps.TryGetValue(key, out var ctxFreeDel))
        {
            var typedDel = (Func<TSource, TDestination>)ctxFreeDel;

            // Use index-based loop when possible (avoids enumerator overhead)
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

        // Use index-based loop for IList<T> to avoid enumerator allocation
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Func<object, object?, ResolutionContext, object> GetDelegate<TSource, TDestination>()
    {
        var key = new TypePair(typeof(TSource), typeof(TDestination));
        if (_config.FrozenMaps.TryGetValue(key, out var del))
            return del;
        throw new InvalidOperationException(
            $"No mapping configured for {typeof(TSource).Name} -> {typeof(TDestination).Name}. " +
            $"Call CreateMap<{typeof(TSource).Name}, {typeof(TDestination).Name}>() in your mapper configuration.");
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
}

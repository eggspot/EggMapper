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
        var del = GetDelegate<TSource, TDestination>();
        var ctx = _sharedCtx ??= new ResolutionContext();
        ctx.Depth = 0;

        var result = source is ICollection<TSource> col
            ? new List<TDestination>(col.Count)
            : new List<TDestination>();

        foreach (var item in source)
        {
            if (item == null) { result.Add(default!); continue; }
            result.Add((TDestination)del(item, null, ctx));
        }
        return result;
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
            return del(source, destination, new ResolutionContext());
        throw new InvalidOperationException(
            $"No mapping configured for {sourceType.Name} -> {destinationType.Name}. " +
            $"Call CreateMap<{sourceType.Name}, {destinationType.Name}>() in your mapper configuration.");
    }
}

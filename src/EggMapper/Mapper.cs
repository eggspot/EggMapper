using System.Runtime.CompilerServices;
using EggMapper.Internal;

namespace EggMapper;

public sealed class Mapper : IMapper
{
    private readonly MapperConfiguration _config;

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
        return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        if (source == null) return destination;
        return (TDestination)MapInternal(source, typeof(TSource), typeof(TDestination), destination);
    }

    public object Map(object source, Type sourceType, Type destinationType)
        => MapInternal(source, sourceType, destinationType, null);

    private object MapInternal(object source, Type sourceType, Type destinationType, object? destination)
    {
        var key = new TypePair(sourceType, destinationType);
        var del = _config.GetMapDelegate(key)
            ?? throw new InvalidOperationException(
                $"No mapping configured for {sourceType.Name} -> {destinationType.Name}. " +
                $"Call CreateMap<{sourceType.Name}, {destinationType.Name}>() in your mapper configuration.");
        return del(source, destination, new ResolutionContext());
    }
}

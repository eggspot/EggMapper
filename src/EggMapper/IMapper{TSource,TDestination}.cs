namespace EggMapper;

/// <summary>
/// Maps one source type to one destination type. Inject it (registered by <c>AddEggMapper</c>) or create it with
/// <see cref="MapperConfiguration.CreateMapper{TSource, TDestination}"/>. Calls are ordinary interface calls, which
/// avoids the generic virtual dispatch of <see cref="IMapper"/>'s generic methods on hot paths.
/// </summary>
/// <typeparam name="TSource">Source type of the configured map.</typeparam>
/// <typeparam name="TDestination">Destination type of the configured map.</typeparam>
public interface IMapper<TSource, TDestination>
{
    /// <summary>Maps <paramref name="source"/> to a new <typeparamref name="TDestination"/>.</summary>
    /// <exception cref="InvalidOperationException">No map is configured for the pair.</exception>
    TDestination Map(TSource source);

    /// <summary>Maps <paramref name="source"/> into <paramref name="destination"/> and returns it.</summary>
    TDestination Map(TSource source, TDestination destination);

    /// <summary>Maps every element of <paramref name="source"/> into a new list.</summary>
    List<TDestination> MapList(IEnumerable<TSource> source);

    /// <summary>Copies only the non-null members of <paramref name="source"/> onto <paramref name="destination"/>.</summary>
    TDestination Patch(TSource source, TDestination destination);
}

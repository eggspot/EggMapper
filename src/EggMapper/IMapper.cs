using System.Collections.Generic;

namespace EggMapper;

public interface IMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
    object Map(object source, Type sourceType, Type destinationType);

    /// <summary>
    /// Maps a collection of <typeparamref name="TSource"/> to a <see cref="List{TDestination}"/>
    /// using a single shared <see cref="ResolutionContext"/>, avoiding per-item allocations.
    /// </summary>
    List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);

    /// <summary>
    /// Partial / patch mapping: copies only non-null (reference types) or HasValue
    /// (Nullable&lt;T&gt;) source properties onto <paramref name="destination"/>.
    /// Non-nullable value-type source properties are always assigned.
    /// </summary>
    TDestination Patch<TSource, TDestination>(TSource source, TDestination destination);
}

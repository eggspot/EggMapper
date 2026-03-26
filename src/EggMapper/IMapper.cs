using System.Collections.Generic;

namespace EggMapper;

public interface IMapper
{
    TDestination Map<TDestination>(object? source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>
    /// Maps <paramref name="source"/> to <typeparamref name="TDestination"/> and then runs any
    /// AfterMap/BeforeMap callbacks registered in <paramref name="opts"/>.
    /// Single-type-arg overload with per-call options:
    /// <c>mapper.Map&lt;TDest&gt;(src, opt =&gt; opt.AfterMap((s, d) =&gt; ...))</c>.
    /// The source parameter of the callback is typed as <c>object</c>; cast it if needed.
    /// </summary>
    TDestination Map<TDestination>(object? source, Action<IMappingOperationOptions<object, TDestination>> opts);

    /// <summary>
    /// Maps <paramref name="source"/> to <typeparamref name="TDestination"/> and then runs any
    /// AfterMap/BeforeMap callbacks registered in <paramref name="opts"/>.
    /// Two-type-arg overload with per-call options:
    /// <c>mapper.Map&lt;TSource, TDest&gt;(src, opt =&gt; opt.AfterMap((s, d) =&gt; ...))</c>.
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts);
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

using System.Collections.Generic;

namespace EggMapper;

/// <summary>
/// Maps objects with the maps of one <c>MapperConfiguration</c>. Create it with <c>config.CreateMapper()</c> or
/// inject it after <c>services.AddEggMapper(...)</c>. Throws <see cref="InvalidOperationException"/> naming the
/// <c>CreateMap</c> to add when a pair has no map.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// Maps <paramref name="source"/> using its runtime type (AutoMapper style). Prefer
    /// <c>Map&lt;TSource, TDestination&gt;</c> when the source type is known: it skips the type lookup.
    /// </summary>
    TDestination Map<TDestination>(object? source);

    /// <summary>
    /// Maps <paramref name="source"/> to a new <typeparamref name="TDestination"/>. The preferred overload; on hot paths
    /// inject <c>IMapper&lt;TSource, TDestination&gt;</c> instead.
    /// </summary>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>Maps <paramref name="source"/> onto the existing <paramref name="destination"/> and returns it.</summary>
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
    /// Copies the same-name members of <paramref name="source"/> whose value is not null onto
    /// <paramref name="destination"/>; non-nullable value types are always copied, so give the patch type nullable
    /// members. Needs a <c>CreateMap&lt;TSource, TDestination&gt;()</c>. Skips expression <c>MapFrom</c>,
    /// <c>Condition</c>, <c>NullSubstitute</c>, resolvers, flattened members, collections and nested objects of a
    /// different type.
    /// </summary>
    TDestination Patch<TSource, TDestination>(TSource source, TDestination destination);
}

namespace EggMapper;

/// <summary>
/// LINQ extension methods for projecting <see cref="IQueryable{T}"/> sequences
/// using registered EggMapper type maps.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Projects each element of <paramref name="source"/> to a
    /// <typeparamref name="TDestination"/> using the type map registered in
    /// <paramref name="config"/>. The projection is built as a pure expression tree
    /// and passed directly to the LINQ provider — no runtime reflection in the query
    /// pipeline, and <c>.Compile()</c> is never called by EggMapper.
    /// Both type arguments are required: <c>query.ProjectTo&lt;Product, ProductDto&gt;(config)</c>.
    /// Translated: same-name and flattened members, nested registered maps, <c>MapFrom</c> expressions, enums by value.
    /// Ignored: child collections, resolvers, <c>Condition</c>, <c>NullSubstitute</c> and nested <c>ConvertUsing</c>.
    /// </summary>
    public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
        this IQueryable<TSource> source, MapperConfiguration config)
    {
        var projection = config.BuildProjection<TSource, TDestination>();
        return source.Select(projection);
    }
}

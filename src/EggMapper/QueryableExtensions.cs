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
    /// </summary>
    public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
        this IQueryable<TSource> source, MapperConfiguration config)
    {
        var projection = config.BuildProjection<TSource, TDestination>();
        return source.Select(projection);
    }
}

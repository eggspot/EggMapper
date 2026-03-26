namespace EggMapper;

/// <summary>
/// Replaces the entire mapping for a source→destination type pair.
/// Implement this interface to fully control the mapping for a type pair.
/// </summary>
public interface ITypeConverter<in TSource, TDestination>
{
    TDestination Convert(TSource source, TDestination? destination, ResolutionContext context);
}

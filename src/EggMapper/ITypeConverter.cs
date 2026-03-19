namespace EggMapper;

/// <summary>
/// Replaces the entire mapping for a source→destination type pair.
/// Compatible with AutoMapper's ITypeConverter pattern.
/// </summary>
public interface ITypeConverter<in TSource, TDestination>
{
    TDestination Convert(TSource source, TDestination? destination, ResolutionContext context);
}

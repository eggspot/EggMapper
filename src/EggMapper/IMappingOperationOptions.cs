namespace EggMapper;

/// <summary>
/// Runtime options passed to the call-site Map overload.
/// Compatible with AutoMapper's IMappingOperationOptions&lt;TSource, TDestination&gt; pattern.
/// </summary>
public interface IMappingOperationOptions<TSource, TDestination>
{
    /// <summary>Arbitrary key/value items accessible within AfterMap/BeforeMap callbacks.</summary>
    IDictionary<string, object> Items { get; }

    void BeforeMap(Action<TSource, TDestination> beforeFunction);
    void AfterMap(Action<TSource, TDestination> afterFunction);
}

namespace EggMapper;

/// <summary>
/// Runtime options passed to the call-site Map overload.
/// Per-call mapping options with BeforeMap/AfterMap callbacks and Items bag.
/// </summary>
public interface IMappingOperationOptions<TSource, TDestination>
{
    /// <summary>Arbitrary key/value items accessible within AfterMap/BeforeMap callbacks.</summary>
    IDictionary<string, object> Items { get; }

    void BeforeMap(Action<TSource, TDestination> beforeFunction);
    void AfterMap(Action<TSource, TDestination> afterFunction);
}

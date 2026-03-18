namespace EggMapper;

public sealed class ResolutionContext
{
    public int Depth { get; internal set; }
    public int MaxDepth { get; internal set; }
    internal Dictionary<object, object> InstanceCache { get; } = new Dictionary<object, object>(ReferenceEqualityObjectComparer.Instance);
}

// Portable reference-equality comparer — works on all target frameworks including
// netstandard2.0 and net462 where ReferenceEqualityComparer is unavailable.
internal sealed class ReferenceEqualityObjectComparer : IEqualityComparer<object>
{
    internal static readonly ReferenceEqualityObjectComparer Instance = new ReferenceEqualityObjectComparer();
    private ReferenceEqualityObjectComparer() { }
    bool IEqualityComparer<object>.Equals(object? x, object? y) => ReferenceEquals(x, y);
    int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
}

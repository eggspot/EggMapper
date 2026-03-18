namespace EggMapper;

public sealed class ResolutionContext
{
    public int Depth { get; internal set; }
    public int MaxDepth { get; internal set; }
    internal Dictionary<object, object> InstanceCache { get; } = new(ReferenceEqualityComparer.Instance);
}

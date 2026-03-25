namespace EggMapper;

public sealed class ResolutionContext
{
    public int Depth { get; internal set; }
    public int MaxDepth { get; internal set; }

    /// <summary>
    /// The mapper instance — allows recursive Map calls from within AfterMap,
    /// BeforeMap, and MapFrom resolution context callbacks.
    /// </summary>
    public IMapper Mapper { get; internal set; } = null!;

    /// <summary>
    /// The service provider — allows resolving DI services (e.g., IMemberValueResolver)
    /// from within mapping delegates. Null when DI is not configured.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; internal set; }

    /// <summary>
    /// Per-call Items dictionary — allows passing context data (e.g., tenant ID)
    /// from the call-site opts delegate into resolvers and mapping callbacks.
    /// </summary>
    public IDictionary<string, object>? Items { get; internal set; }

    // Allocated only when cycle-detection is actually needed; most simple mappings
    // never touch this dictionary, so we avoid the allocation on every Map call.
    private Dictionary<object, object>? _instanceCache;
    internal Dictionary<object, object> InstanceCache =>
        _instanceCache ??= new Dictionary<object, object>(ReferenceEqualityObjectComparer.Instance);

    /// <summary>
    /// Clears the instance cache between top-level Map calls so stale references
    /// from previous requests on the same thread don't leak (ThreadStatic reuse).
    /// </summary>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    internal void ClearInstanceCache()
    {
        if (_instanceCache != null && _instanceCache.Count > 0)
            _instanceCache.Clear();
    }
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

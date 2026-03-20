using System.Reflection;

namespace EggMapper;

internal sealed class PropertyMap
{
    public PropertyInfo DestinationProperty { get; set; } = null!;
    public bool Ignored { get; set; }
    public Func<object, object?, object?>? CustomResolver { get; set; }
    // Context-aware resolver: (src, dest, destMember, ctx) => value
    public Func<object, object?, object?, ResolutionContext, object?>? ContextResolver { get; set; }
    public Func<object, bool>? Condition { get; set; }
    public Func<object, object?, bool>? FullCondition { get; set; }
    public Func<object, bool>? PreCondition { get; set; }
    public object? NullSubstitute { get; set; }
    public bool HasNullSubstitute { get; set; }
    public object? UseValue { get; set; }
    public bool HasUseValue { get; set; }
    public bool UseDestinationValue { get; set; }
    public string? SourceMemberName { get; set; }
    // DI-based value resolver factory: (IServiceProvider) => resolver func
    public Func<IServiceProvider, Func<object, object?, object?, ResolutionContext, object?>>? ValueResolverFactory { get; set; }
}

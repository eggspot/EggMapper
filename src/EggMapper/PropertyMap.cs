using System.Reflection;

namespace EggMapper;

internal sealed class PropertyMap
{
    public PropertyInfo DestinationProperty { get; set; } = null!;
    public bool Ignored { get; set; }
    public Func<object, object?, object?>? CustomResolver { get; set; }
    public Func<object, bool>? Condition { get; set; }
    public Func<object, object?, bool>? FullCondition { get; set; }
    public Func<object, bool>? PreCondition { get; set; }
    public object? NullSubstitute { get; set; }
    public bool HasNullSubstitute { get; set; }
    public object? UseValue { get; set; }
    public bool HasUseValue { get; set; }
    public string? SourceMemberName { get; set; }
}

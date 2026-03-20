using EggMapper.Internal;

namespace EggMapper;

internal sealed class TypeMap
{
    public Type SourceType { get; set; } = null!;
    public Type DestinationType { get; set; } = null!;
    public List<PropertyMap> PropertyMaps { get; set; } = new();
    public Func<object, object>? CustomConstructor { get; set; }
    public Action<object, object>? BeforeMapAction { get; set; }
    public Action<object, object>? AfterMapAction { get; set; }
    // Context-aware hooks (3-arg: src, dest, ResolutionContext)
    public Action<object, object, ResolutionContext>? BeforeMapCtxAction { get; set; }
    public Action<object, object, ResolutionContext>? AfterMapCtxAction { get; set; }
    public TypePair? BaseMapTypePair { get; set; }
    public int MaxDepth { get; set; }
    public bool HasReverseMap { get; set; }
    public bool IncludeAllDerivedFlag { get; set; }
    // ConvertUsing: replaces the entire mapping with a custom converter
    public Func<object, object?, ResolutionContext, object>? ConvertUsingFunc { get; set; }
    public Func<object, object?, ResolutionContext, object>? MappingDelegate { get; set; }
    public Func<System.Reflection.PropertyInfo, bool>? ShouldMapProperty { get; set; }
}

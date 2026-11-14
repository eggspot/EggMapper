using System.Linq.Expressions;
using EggMapper.Internal;

namespace EggMapper;

internal sealed class TypeMap
{
    public Type SourceType { get; set; } = null!;
    public Type DestinationType { get; set; } = null!;
    public List<PropertyMap> PropertyMaps { get; set; } = new();
    public Func<object, object>? CustomConstructor { get; set; }
    public Func<object, ResolutionContext, object>? CustomConstructorWithCtx { get; set; }
    public Action<object, object>? BeforeMapAction { get; set; }
    public Action<object, object>? AfterMapAction { get; set; }
    public Action<object, object, ResolutionContext>? BeforeMapCtxAction { get; set; }
    public Action<object, object, ResolutionContext>? AfterMapCtxAction { get; set; }
    public TypePair? BaseMapTypePair { get; set; }
    public List<PropertyMap>? EffectivePropertyMaps { get; set; }
    public int MaxDepth { get; set; }
    public bool HasReverseMap { get; set; }
    public bool IncludeAllDerivedFlag { get; set; }
    public Func<object, object?, ResolutionContext, object>? ConvertUsingFunc { get; set; }
    public Type? OpenGenericConverterType { get; set; }
    public Func<System.Reflection.PropertyInfo, bool>? ShouldMapProperty { get; set; }
    public LambdaExpression? MappingExpression { get; set; }
    public string[]? ReusedConstructorMembers { get; set; }

    public List<(Func<object, bool> Predicate, string Message)>? ValidationRules { get; set; }
}

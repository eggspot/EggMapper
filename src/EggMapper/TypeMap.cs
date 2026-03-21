using System.Linq.Expressions;
using EggMapper.Internal;

namespace EggMapper;

internal sealed class TypeMap
{
    public Type SourceType { get; set; } = null!;
    public Type DestinationType { get; set; } = null!;
    public List<PropertyMap> PropertyMaps { get; set; } = new();
    public Func<object, object>? CustomConstructor { get; set; }
    /// <summary>Context-aware constructor — takes precedence over <see cref="CustomConstructor"/>.</summary>
    public Func<object, ResolutionContext, object>? CustomConstructorWithCtx { get; set; }
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
    /// <summary>The compiled expression tree, stored before .Compile() for diagnostics.</summary>
    public LambdaExpression? MappingExpression { get; set; }

    /// <summary>
    /// Inline validation rules added via .Validate(). Each entry is a (predicate, errorMessage)
    /// pair where the predicate receives the fully-mapped destination object (boxed as object).
    /// Maps with validators are routed to the flexible path (zero overhead when list is null).
    /// </summary>
    public List<(Func<object, bool> Predicate, string Message)>? ValidationRules { get; set; }
}

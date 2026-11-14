namespace EggMapper;

/// <summary>Enum-pair configuration for <see cref="IMappingExpression{TSource, TDestination}"/>.</summary>
public static class EnumMappingExpressionExtensions
{
    /// <summary>
    /// Maps this enum pair by name, by value, or with explicit value overrides, instead of the global
    /// <see cref="IMapperConfigurationExpression.EnumMapping"/> mode. Same shape as AutoMapper.Extensions.EnumMapping.
    /// A <c>ConvertUsing</c> on the same pair takes precedence.
    /// </summary>
    public static IMappingExpression<TSourceEnum, TDestinationEnum> ConvertUsingEnumMapping<TSourceEnum, TDestinationEnum>(
        this IMappingExpression<TSourceEnum, TDestinationEnum> mappingExpression,
        Action<EnumMappingOptions<TSourceEnum, TDestinationEnum>> configure)
        where TSourceEnum : struct, Enum
        where TDestinationEnum : struct, Enum
    {
        var options = new EnumMappingOptions<TSourceEnum, TDestinationEnum>();
        configure(options);
        if (mappingExpression is MappingExpression<TSourceEnum, TDestinationEnum> concrete)
            concrete.SetEnumMapping(options.ToPairMapping());
        return mappingExpression;
    }
}

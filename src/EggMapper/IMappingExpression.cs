using System.Linq.Expressions;

namespace EggMapper;

public interface IMappingExpression<TSource, TDestination>
{
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

    IMappingExpression<TSource, TDestination> ForPath<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IPathConfigurationExpression<TSource, TDestination, TMember>> pathOptions);

    IMappingExpression<TSource, TDestination> ReverseMap();

    IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor);

    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction);
    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, ResolutionContext> beforeFunction);

    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction);
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, ResolutionContext> afterFunction);

    IMappingExpression<TSource, TDestination> IncludeBase<TSourceBase, TDestinationBase>();
    IMappingExpression<TSource, TDestination> IncludeAllDerived();

    IMappingExpression<TSource, TDestination> MaxDepth(int depth);

    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination?, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(Func<TSource, TDestination?, ResolutionContext, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing(ITypeConverter<TSource, TDestination> converter);
    IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>() where TConverter : ITypeConverter<TSource, TDestination>, new();
    IMappingExpression<TSource, TDestination> ConvertUsing(Type converterType);

    IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);
}

/// <summary>
/// Non-generic mapping expression for CreateMap(Type, Type) scenarios.
/// </summary>
public interface IMappingExpression
{
    IMappingExpression ForMember(string destinationMember, Action<IMemberConfigurationExpression> memberOptions);
    IMappingExpression IncludeAllDerived();
    IMappingExpression ConvertUsing(Type converterType);
}

/// <summary>
/// Non-generic member configuration for string-based ForMember.
/// </summary>
public interface IMemberConfigurationExpression
{
    void MapFrom(string sourceMemberName);
    void Ignore();
}

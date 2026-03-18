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

    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction);

    IMappingExpression<TSource, TDestination> IncludeBase<TSourceBase, TDestinationBase>();

    IMappingExpression<TSource, TDestination> MaxDepth(int depth);

    IMappingExpression<TSource, TDestination> ForAllMembers(
        Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);
}

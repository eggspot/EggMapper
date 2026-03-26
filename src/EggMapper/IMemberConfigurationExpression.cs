using System.Linq.Expressions;

namespace EggMapper;

public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> mapExpression);
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> mapFunction);
    // 3-arg: (src, dest, destMember) => value
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TMember, TSourceMember> mapFunction);
    // 4-arg with context: (src, dest, destMember, context) => value
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TMember, ResolutionContext, TSourceMember> mapFunction);
    /// <summary>
    /// Resolve the destination member from a source property by name.
    /// String-based overload: <c>MapFrom("SourcePropertyName")</c>.
    /// </summary>
    void MapFrom(string sourceMemberName);

    /// <summary>
    /// Resolve the destination member using a DI-injected <see cref="IMemberValueResolver{TSource,TDestination,TSourceMember,TDestMember}"/>.
    /// The resolver is resolved from the service provider at mapping time.
    /// </summary>
    void MapFrom<TValueResolver, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
        where TValueResolver : IMemberValueResolver<object, object, TSourceMember, TMember>;

    void Ignore();
    void Condition(Func<TSource, bool> condition);
    void Condition(Func<TSource, TDestination, bool> condition);
    void PreCondition(Func<TSource, bool> condition);
    void NullSubstitute(TMember nullSubstitute);
    void UseValue(TMember value);
    void UseDestinationValue();
}

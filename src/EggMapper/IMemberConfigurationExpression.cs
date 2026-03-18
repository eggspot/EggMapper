using System.Linq.Expressions;

namespace EggMapper;

public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> mapExpression);
    void MapFrom<TSourceMember>(Func<TSource, TDestination, TSourceMember> mapFunction);
    void Ignore();
    void Condition(Func<TSource, bool> condition);
    void Condition(Func<TSource, TDestination, bool> condition);
    void PreCondition(Func<TSource, bool> condition);
    void NullSubstitute(TMember nullSubstitute);
    void UseValue(TMember value);
}

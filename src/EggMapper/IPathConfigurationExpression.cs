using System.Linq.Expressions;

namespace EggMapper;

public interface IPathConfigurationExpression<TSource, TDestination, TMember>
{
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> mapExpression);
    void Ignore();
}

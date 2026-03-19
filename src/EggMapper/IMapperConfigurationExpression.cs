using System.Reflection;

namespace EggMapper;

public interface IMapperConfigurationExpression
{
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
    void AddProfile<TProfile>() where TProfile : Profile, new();
    void AddProfile(Profile profile);
    void AddProfiles(IEnumerable<Assembly> assemblies);
    /// <summary>
    /// Scans assemblies for Profile classes and registers them.
    /// AutoMapper-compatible alias for AddProfiles.
    /// </summary>
    void AddMaps(params Assembly[] assemblies);
    void AddMaps(IEnumerable<Assembly> assemblies);
}

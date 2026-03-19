using System.Reflection;

namespace EggMapper;

public interface IMapperConfigurationExpression
{
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();

    /// <summary>
    /// Non-generic CreateMap for open generic or runtime type scenarios.
    /// </summary>
    IMappingExpression CreateMap(Type sourceType, Type destinationType);

    void AddProfile<TProfile>() where TProfile : Profile, new();
    void AddProfile(Profile profile);
    void AddProfiles(IEnumerable<Assembly> assemblies);

    void AddMaps(params Assembly[] assemblies);
    void AddMaps(IEnumerable<Assembly> assemblies);
    void AddMaps(params Type[] markerTypes);

    /// <summary>
    /// Filter which properties should be mapped. Return false to skip a property.
    /// Compatible with AutoMapper's ShouldMapProperty.
    /// </summary>
    Func<PropertyInfo, bool>? ShouldMapProperty { get; set; }
}

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

    /// <summary>
    /// Global maximum recursion depth applied as a safety net in the flexible delegate path.
    /// Default is 32. Set to 0 to disable. Individual maps can override with .MaxDepth(n).
    /// Prevents stack exhaustion from deeply or infinitely nested object graphs (CVE-class issue).
    /// </summary>
    int DefaultMaxDepth { get; set; }

    /// <summary>
    /// Registers a global type converter that applies automatically whenever a source property
    /// of type <typeparamref name="TSource"/> must be assigned to a destination property of
    /// type <typeparamref name="TDest"/> and no per-map <c>ForMember</c> override is present.
    /// The converter is inlined into the compiled expression tree — no boxing, no extra allocations.
    /// </summary>
    void AddTypeConverter<TSource, TDest>(Func<TSource, TDest> converter);
}

using System.Reflection;

namespace EggMapper;

/// <summary>
/// Registers maps inside <c>new MapperConfiguration(cfg =&gt; ...)</c> or <c>services.AddEggMapper(cfg =&gt; ...)</c>.
/// </summary>
public interface IMapperConfigurationExpression
{
    /// <summary>
    /// Registers the map from <typeparamref name="TSource"/> to <typeparamref name="TDestination"/>; a later
    /// <c>CreateMap</c> for the same pair replaces it. Register nested types too: a nested object of a different type
    /// without its own map is left null.
    /// </summary>
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();

    /// <summary>
    /// Non-generic CreateMap for open generic or runtime type scenarios.
    /// </summary>
    IMappingExpression CreateMap(Type sourceType, Type destinationType);

    void AddProfile<TProfile>() where TProfile : Profile, new();
    void AddProfile(Profile profile);
    void AddProfiles(IEnumerable<Assembly> assemblies);

    /// <summary>
    /// Registers every <see cref="Profile"/> in <paramref name="assemblies"/>. When two profiles map the same pair, the
    /// first one registered wins and <c>AssertConfigurationIsValid</c> reports the duplicate. An explicit
    /// <c>CreateMap</c> always wins over a map created by <c>ReverseMap()</c>.
    /// </summary>
    void AddMaps(params Assembly[] assemblies);
    void AddMaps(IEnumerable<Assembly> assemblies);
    void AddMaps(params Type[] markerTypes);

    /// <summary>
    /// Filter which properties should be mapped. Return false to skip a property.
    /// Filter predicate applied during configuration to skip unwanted properties.
    /// </summary>
    Func<PropertyInfo, bool>? ShouldMapProperty { get; set; }

    /// <summary>
    /// How a value of one enum type maps to a different enum type when the pair has no
    /// <c>ConvertUsingEnumMapping</c>, <c>ConvertUsing</c> or type converter. Defaults to <see cref="EnumMappingMode.ByName"/>,
    /// AutoMapper's behavior. <c>ProjectTo</c> always maps enums by value.
    /// </summary>
    EnumMappingMode EnumMapping { get; set; }

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

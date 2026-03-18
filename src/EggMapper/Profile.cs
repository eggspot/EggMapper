namespace EggMapper;

public abstract class Profile
{
    private readonly MapperConfigurationExpression _cfg = new();

    public string ProfileName => GetType().Name;

    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        => _cfg.CreateMap<TSource, TDestination>();

    internal IEnumerable<TypeMap> GetTypeMaps() => _cfg.GetTypeMaps();
}

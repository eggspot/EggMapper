using System.Reflection;
using EggMapper;

namespace Microsoft.Extensions.DependencyInjection;

public static class EggMapperServiceCollectionExtensions
{
    public static IServiceCollection AddEggMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfiles(assemblies));
        services.AddSingleton(config);
        services.AddSingleton<IMapper>(sp =>
        {
            var mapper = (Mapper)sp.GetRequiredService<MapperConfiguration>().CreateMapper();
            mapper.ServiceProvider = sp;
            return mapper;
        });
        return services;
    }

    public static IServiceCollection AddEggMapper(this IServiceCollection services, Action<IMapperConfigurationExpression> configure)
    {
        var config = new MapperConfiguration(configure);
        services.AddSingleton(config);
        services.AddSingleton<IMapper>(sp =>
        {
            var mapper = (Mapper)sp.GetRequiredService<MapperConfiguration>().CreateMapper();
            mapper.ServiceProvider = sp;
            return mapper;
        });
        return services;
    }
}

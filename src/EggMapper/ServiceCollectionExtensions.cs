using System.Reflection;
using EggMapper;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class EggMapperServiceCollectionExtensions
{
    public static IServiceCollection AddEggMapper(this IServiceCollection services, params Assembly[] assemblies)
        => AddMapperServices(services, new MapperConfiguration(cfg => cfg.AddProfiles(assemblies)));

    public static IServiceCollection AddEggMapper(this IServiceCollection services, Action<IMapperConfigurationExpression> configure)
        => AddMapperServices(services, new MapperConfiguration(configure));

    private static IServiceCollection AddMapperServices(IServiceCollection services, MapperConfiguration config)
    {
        services.AddSingleton(config);
        services.AddTransient<IMapper>(sp =>
        {
            var mapper = (Mapper)sp.GetRequiredService<MapperConfiguration>().CreateMapper();
            mapper.ServiceProvider = sp;
            return mapper;
        });
        services.TryAddTransient(typeof(IMapper<,>), typeof(TypedMapper<,>));
        return services;
    }
}

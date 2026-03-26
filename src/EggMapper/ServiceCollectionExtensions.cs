using System.Reflection;
using EggMapper;

namespace Microsoft.Extensions.DependencyInjection;

public static class EggMapperServiceCollectionExtensions
{
    public static IServiceCollection AddEggMapper(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfiles(assemblies));
        services.AddSingleton(config);
        // Transient: each injection gets a fresh Mapper with the caller's IServiceProvider.
        // This is critical for correct scoped service resolution in DI value resolvers
        // (e.g., DbContext, IMediaAssetService).
        // Mapper is lightweight (~32 bytes wrapping the singleton config) so per-injection
        // allocation cost is negligible.
        // Works correctly in all hosting models:
        //   - ASP.NET API/MVC: per-controller injection = per-request scope
        //   - Blazor Server: per-component injection = circuit scope
        //   - Windows Service: per-scope when using IServiceScopeFactory
        //   - gRPC: per-call scope
        services.AddTransient<IMapper>(sp =>
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
        services.AddTransient<IMapper>(sp =>
        {
            var mapper = (Mapper)sp.GetRequiredService<MapperConfiguration>().CreateMapper();
            mapper.ServiceProvider = sp;
            return mapper;
        });
        return services;
    }
}

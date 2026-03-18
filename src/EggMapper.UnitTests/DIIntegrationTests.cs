using System.Reflection;
using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class DIIntegrationTests
{
    [Fact]
    public void AddEggMapper_configure_registers_IMapper_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddEggMapper(cfg => cfg.CreateMap<FlatSource, FlatDest>());

        using var provider = services.BuildServiceProvider();
        var mapper1 = provider.GetRequiredService<IMapper>();
        var mapper2 = provider.GetRequiredService<IMapper>();

        mapper1.Should().NotBeNull();
        mapper1.Should().BeSameAs(mapper2);
    }

    [Fact]
    public void AddEggMapper_configure_IMapper_can_map_objects()
    {
        var services = new ServiceCollection();
        services.AddEggMapper(cfg => cfg.CreateMap<FlatSource, FlatDest>());

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var src = new FlatSource { Name = "DI Test", Age = 42 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("DI Test");
        dest.Age.Should().Be(42);
    }

    [Fact]
    public void AddEggMapper_assemblies_scans_and_registers_profiles()
    {
        var services = new ServiceCollection();
        services.AddEggMapper(Assembly.GetExecutingAssembly());

        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var src = new FlatSource { Name = "Scanned", Age = 7 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Scanned");
    }

    [Fact]
    public void AddEggMapper_registers_MapperConfiguration_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddEggMapper(cfg => cfg.CreateMap<FlatSource, FlatDest>());

        using var provider = services.BuildServiceProvider();
        var config1 = provider.GetRequiredService<MapperConfiguration>();
        var config2 = provider.GetRequiredService<MapperConfiguration>();

        config1.Should().NotBeNull();
        config1.Should().BeSameAs(config2);
    }
}

using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class AddMapsTests
{
    [Fact]
    public void AddMaps_ScansAssemblyForProfiles()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddMaps(typeof(AddMapsTests).Assembly));
        var mapper = config.CreateMapper();

        // ProfileTests already defines test profiles in this assembly.
        // This test just verifies AddMaps works as an alias for AddProfiles.
        mapper.Should().NotBeNull();
    }

    [Fact]
    public void AddMaps_MultipleAssemblies()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddMaps(typeof(AddMapsTests).Assembly, typeof(MapperConfiguration).Assembly));
        var mapper = config.CreateMapper();
        mapper.Should().NotBeNull();
    }
}

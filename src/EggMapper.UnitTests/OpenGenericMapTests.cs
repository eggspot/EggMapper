using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class OpenGenericMapTests
{
    [Fact]
    public void CreateMap_NonGeneric_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap(typeof(OgSource), typeof(OgDest)));
        var mapper = config.CreateMapper();

        var result = mapper.Map<OgSource, OgDest>(new OgSource { Name = "Hello" });
        result.Name.Should().Be("Hello");
    }

    [Fact]
    public void CreateMap_NonGeneric_WithForMemberByString()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap(typeof(OgSource), typeof(OgDest))
               .ForMember("Name", o => o.MapFrom("Name")));
        var mapper = config.CreateMapper();

        var result = mapper.Map<OgSource, OgDest>(new OgSource { Name = "World" });
        result.Name.Should().Be("World");
    }

    [Fact]
    public void CreateMap_NonGeneric_ReturnsNonGenericExpression()
    {
        var config = new MapperConfiguration(cfg =>
        {
            var expr = cfg.CreateMap(typeof(OgSource), typeof(OgDest));
            expr.Should().NotBeNull();
        });
        var mapper = config.CreateMapper();
        mapper.Should().NotBeNull();
    }
}

file class OgSource { public string Name { get; set; } = ""; }
file class OgDest { public string Name { get; set; } = ""; }

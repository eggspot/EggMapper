using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ShouldMapPropertyTests
{
    [Fact]
    public void ShouldMapProperty_False_SkipsAllProperties()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.ShouldMapProperty = p => false;
            cfg.CreateMap<SmpSource, SmpDest>();
        });

        // With ShouldMapProperty = false, AssertConfigurationIsValid should
        // not complain about unmapped members (they are intentionally skipped)
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void ShouldMapProperty_FilterByType_OnlyMapsStrings()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.ShouldMapProperty = p => p.PropertyType == typeof(string);
            cfg.CreateMap<SmpSource, SmpDest>();
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<SmpSource, SmpDest>(new SmpSource { Name = "Test", Value = 42 });
        result.Name.Should().Be("Test");
        result.Value.Should().Be(0); // int property not mapped
    }
}

file class SmpSource { public string Name { get; set; } = ""; public int Value { get; set; } }
file class SmpDest { public string Name { get; set; } = ""; public int Value { get; set; } }

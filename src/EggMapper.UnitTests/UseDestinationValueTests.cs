using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class UseDestinationValueTests
{
    [Fact]
    public void UseDestinationValue_PreservesExistingValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<UdvSource, UdvDest>()
               .ForMember(d => d.Locked, o => o.UseDestinationValue()));
        var mapper = config.CreateMapper();

        var src = new UdvSource { Name = "New", Locked = "ShouldBeIgnored" };
        var dest = new UdvDest { Name = "Old", Locked = "KeepMe" };

        var result = mapper.Map(src, dest);
        result.Name.Should().Be("New");
        result.Locked.Should().Be("KeepMe"); // preserved
    }

    [Fact]
    public void UseDestinationValue_WithNullDestination_UsesDefault()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<UdvSource, UdvDest>()
               .ForMember(d => d.Locked, o => o.UseDestinationValue()));
        var mapper = config.CreateMapper();

        var result = mapper.Map<UdvSource, UdvDest>(new UdvSource { Name = "Test", Locked = "X" });
        result.Name.Should().Be("Test");
        result.Locked.Should().Be(""); // default from new UdvDest()
    }
}

file class UdvSource { public string Name { get; set; } = ""; public string Locked { get; set; } = ""; }
file class UdvDest { public string Name { get; set; } = ""; public string Locked { get; set; } = ""; }

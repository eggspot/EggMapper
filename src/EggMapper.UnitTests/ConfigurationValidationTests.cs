using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class ValidSource { public string Name { get; set; } = ""; public int Age { get; set; } }
file class ValidDest { public string Name { get; set; } = ""; public int Age { get; set; } }
file class InvalidDest { public string Name { get; set; } = ""; public string Unmatched { get; set; } = ""; }
file class PartialSource { public string Name { get; set; } = ""; }

public class ConfigurationValidationTests
{
    [Fact]
    public void AssertConfigurationIsValid_passes_for_fully_matched_map()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ValidSource, ValidDest>());

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void AssertConfigurationIsValid_throws_for_unmapped_dest_member()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<PartialSource, InvalidDest>());

        var act = () => config.AssertConfigurationIsValid();
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Unmatched*");
    }

    [Fact]
    public void AssertConfigurationIsValid_passes_when_unmapped_member_is_ignored()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PartialSource, InvalidDest>()
               .ForMember(d => d.Unmatched, opts => opts.Ignore());
        });

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void AssertConfigurationIsValid_passes_when_unmapped_member_has_custom_resolver()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PartialSource, InvalidDest>()
               .ForMember(d => d.Unmatched, opts => opts.MapFrom(s => "constant"));
        });

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void AssertConfigurationIsValid_passes_when_unmapped_member_has_use_value()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PartialSource, InvalidDest>()
               .ForMember(d => d.Unmatched, opts => opts.UseValue("fixed"));
        });

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }
}

using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class ValidSource { public string Name { get; set; } = ""; public int Age { get; set; } }
file class ValidDest { public string Name { get; set; } = ""; public int Age { get; set; } }
file class InvalidDest { public string Name { get; set; } = ""; public string Unmatched { get; set; } = ""; }
file class PartialSource { public string Name { get; set; } = ""; }

file class TheBase { public string TheName { get; set; } = ""; }
file class BaseDto { public string Name { get; set; } = ""; }
file class Child : TheBase { public int MyProperty { get; set; } }
file class ChildDto : BaseDto { public int MyProperty { get; set; } }

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

    [Fact]
    public void AssertConfigurationIsValid_passes_for_member_mapped_via_IncludeBase()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TheBase, BaseDto>()
               .ForMember(dto => dto.Name, map => map.MapFrom(src => src.TheName));
            cfg.CreateMap<Child, ChildDto>().IncludeBase<TheBase, BaseDto>();
        });

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();

        var mapper = config.CreateMapper();
        var result = mapper.Map<ChildDto>(new Child { TheName = "aaa" });
        result.Name.Should().Be("aaa");
    }
}

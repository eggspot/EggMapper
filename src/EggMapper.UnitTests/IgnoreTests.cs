using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class SourceMulti { public string A { get; set; } = ""; public string B { get; set; } = ""; public string C { get; set; } = ""; }
file class DestMulti { public string A { get; set; } = ""; public string B { get; set; } = ""; public string C { get; set; } = ""; }

public class IgnoreTests
{
    [Fact]
    public void Ignored_member_stays_at_default_value()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts => opts.Ignore());
        }).CreateMapper();

        var src = new FlatSource { Name = "ShouldBeIgnored", Age = 10 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().BeEmpty();
        dest.Age.Should().Be(10);
    }

    [Fact]
    public void Ignored_member_does_not_fail_validation()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SimpleSource, FlatDest>()
               .ForMember(d => d.Age, opts => opts.Ignore())
               .ForMember(d => d.Value, opts => opts.Ignore())
               .ForMember(d => d.Email, opts => opts.Ignore())
               .ForMember(d => d.IsActive, opts => opts.Ignore())
               .ForMember(d => d.Name, opts => opts.MapFrom(s => s.Value));
        });

        var act = () => config.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }

    [Fact]
    public void Multiple_ignored_members_all_stay_default()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceMulti, DestMulti>()
               .ForMember(d => d.A, opts => opts.Ignore())
               .ForMember(d => d.B, opts => opts.Ignore());
        }).CreateMapper();

        var src = new SourceMulti { A = "AA", B = "BB", C = "CC" };
        var dest = mapper.Map<SourceMulti, DestMulti>(src);
        dest.A.Should().BeEmpty();
        dest.B.Should().BeEmpty();
        dest.C.Should().Be("CC");
    }

    [Fact]
    public void Ignored_member_not_overwritten_by_convention()
    {
        // Verify that Ignore truly prevents convention mapping too
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Age, opts => opts.Ignore());
        }).CreateMapper();

        var src = new FlatSource { Name = "Alice", Age = 999 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(0);
        dest.Name.Should().Be("Alice");
    }
}

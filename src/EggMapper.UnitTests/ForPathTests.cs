using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ForPathTests
{
    [Fact]
    public void ForPath_MapFrom_maps_nested_source_to_flat_destination_property()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NestedPathSource, ForPathDest>()
               .ForPath(d => d.InnerValue, opts => opts.MapFrom(s => s.Inner != null ? s.Inner.Value : null))
               .ForPath(d => d.InnerCount, opts => opts.MapFrom(s => s.Inner != null ? s.Inner.Count : 0));
        }).CreateMapper();

        var src = new NestedPathSource { Inner = new InnerSource { Value = "deep", Count = 7 } };
        var dest = mapper.Map<NestedPathSource, ForPathDest>(src);
        dest.InnerValue.Should().Be("deep");
        dest.InnerCount.Should().Be(7);
    }

    [Fact]
    public void ForPath_can_map_top_level_source_property()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NestedPathSource, ForPathDest>()
               .ForPath(d => d.TopLevel, opts => opts.MapFrom(s => s.TopLevel))
               .ForPath(d => d.InnerValue, opts => opts.Ignore())
               .ForPath(d => d.InnerCount, opts => opts.Ignore());
        }).CreateMapper();

        var src = new NestedPathSource { TopLevel = "top", Inner = new InnerSource() };
        var dest = mapper.Map<NestedPathSource, ForPathDest>(src);
        dest.TopLevel.Should().Be("top");
    }

    [Fact]
    public void ForPath_Ignore_leaves_property_at_default()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NestedPathSource, ForPathDest>()
               .ForPath(d => d.InnerValue, opts => opts.Ignore())
               .ForPath(d => d.InnerCount, opts => opts.Ignore())
               .ForPath(d => d.TopLevel, opts => opts.MapFrom(s => s.TopLevel));
        }).CreateMapper();

        var src = new NestedPathSource { Inner = new InnerSource { Value = "X" }, TopLevel = "T" };
        var dest = mapper.Map<NestedPathSource, ForPathDest>(src);
        dest.InnerValue.Should().BeNull();
        dest.InnerCount.Should().Be(0);
    }

    [Fact]
    public void ForPath_with_null_nested_source_does_not_throw()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NestedPathSource, ForPathDest>()
               .ForPath(d => d.InnerValue, opts => opts.MapFrom(s => s.Inner != null ? s.Inner.Value : null))
               .ForPath(d => d.InnerCount, opts => opts.MapFrom(s => s.Inner != null ? s.Inner.Count : 0));
        }).CreateMapper();

        var src = new NestedPathSource { Inner = null };
        var act = () => mapper.Map<NestedPathSource, ForPathDest>(src);
        act.Should().NotThrow();
        var dest = act();
        dest.InnerValue.Should().BeNull();
        dest.InnerCount.Should().Be(0);
    }
}

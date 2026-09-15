using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

// Base map has an explicit ForMember so IncludeBase can copy it
file class BaseSrcWithCustom { public string BaseProp { get; set; } = ""; }
file class BaseDestWithCustom { public string BaseProp { get; set; } = ""; }
file class DerivedSrcWithCustom : BaseSrcWithCustom { public string Extra { get; set; } = ""; }
file class DerivedDestWithCustom : BaseDestWithCustom { public string Extra { get; set; } = ""; }

// Three-level IncludeBase chain: Leaf -> Mid -> GrandBase, custom ForMember only on GrandBase
file class GrandBaseSrc { public string TheName { get; set; } = ""; }
file class GrandBaseDto { public string Name { get; set; } = ""; }
file class MidSrc : GrandBaseSrc { public int MidProp { get; set; } }
file class MidDto : GrandBaseDto { public int MidProp { get; set; } }
file class LeafSrc : MidSrc { public int LeafProp { get; set; } }
file class LeafDto : MidDto { public int LeafProp { get; set; } }

public class InheritanceMappingTests
{
    [Fact]
    public void Base_class_properties_are_mapped_by_convention()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BaseSource, BaseDest>();
            cfg.CreateMap<DerivedSource, DerivedDest>();
        }).CreateMapper();

        var src = new DerivedSource { BaseProp = "base-val", DerivedProp = "derived-val" };
        var dest = mapper.Map<DerivedSource, DerivedDest>(src);
        dest.BaseProp.Should().Be("base-val");
        dest.DerivedProp.Should().Be("derived-val");
    }

    [Fact]
    public void IncludeBase_applies_base_ForMember_to_derived_map()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BaseSrcWithCustom, BaseDestWithCustom>()
               .ForMember(d => d.BaseProp, opts => opts.MapFrom(s => s.BaseProp.ToUpper()));

            cfg.CreateMap<DerivedSrcWithCustom, DerivedDestWithCustom>()
               .IncludeBase<BaseSrcWithCustom, BaseDestWithCustom>();
        }).CreateMapper();

        var src = new DerivedSrcWithCustom { BaseProp = "hello", Extra = "world" };
        var dest = mapper.Map<DerivedSrcWithCustom, DerivedDestWithCustom>(src);

        dest.BaseProp.Should().Be("HELLO");
        dest.Extra.Should().Be("world");
    }

    [Fact]
    public void Derived_map_convention_still_applies_for_derived_properties()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BaseSource, BaseDest>();
            cfg.CreateMap<DerivedSource, DerivedDest>()
               .IncludeBase<BaseSource, BaseDest>();
        }).CreateMapper();

        var src = new DerivedSource { BaseProp = "B", DerivedProp = "D" };
        var dest = mapper.Map<DerivedSource, DerivedDest>(src);
        dest.BaseProp.Should().Be("B");
        dest.DerivedProp.Should().Be("D");
    }

    [Fact]
    public void Base_map_works_independently()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BaseSource, BaseDest>();
            cfg.CreateMap<DerivedSource, DerivedDest>().IncludeBase<BaseSource, BaseDest>();
        }).CreateMapper();

        var src = new BaseSource { BaseProp = "base-only" };
        var dest = mapper.Map<BaseSource, BaseDest>(src);
        dest.BaseProp.Should().Be("base-only");
    }

    [Fact]
    public void IncludeBase_does_not_override_derived_explicit_ForMember()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<BaseSrcWithCustom, BaseDestWithCustom>()
               .ForMember(d => d.BaseProp, opts => opts.MapFrom(s => "from-base"));

            cfg.CreateMap<DerivedSrcWithCustom, DerivedDestWithCustom>()
               .ForMember(d => d.BaseProp, opts => opts.MapFrom(s => "from-derived"))
               .IncludeBase<BaseSrcWithCustom, BaseDestWithCustom>();
        }).CreateMapper();

        var src = new DerivedSrcWithCustom { BaseProp = "original" };
        var dest = mapper.Map<DerivedSrcWithCustom, DerivedDestWithCustom>(src);
        dest.BaseProp.Should().Be("from-derived");
    }

    [Fact]
    public void IncludeBase_applies_ForMember_transitively_across_multiple_levels()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrandBaseSrc, GrandBaseDto>()
               .ForMember(d => d.Name, opts => opts.MapFrom(s => s.TheName));

            cfg.CreateMap<MidSrc, MidDto>().IncludeBase<GrandBaseSrc, GrandBaseDto>();

            cfg.CreateMap<LeafSrc, LeafDto>().IncludeBase<MidSrc, MidDto>();
        }).CreateMapper();

        var src = new LeafSrc { TheName = "aaa", MidProp = 1, LeafProp = 2 };
        var dest = mapper.Map<LeafSrc, LeafDto>(src);

        dest.Name.Should().Be("aaa");
        dest.MidProp.Should().Be(1);
        dest.LeafProp.Should().Be(2);
    }
}

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

// Base-level mapping that a derived-level Condition depends on via the destination object
file class OrderBaseSrc { public string Status { get; set; } = ""; }
file class OrderBaseDto { public string Status { get; set; } = ""; }
file class OrderDerivedSrc : OrderBaseSrc { public int Bonus { get; set; } }
file class OrderDerivedDto : OrderBaseDto { public int Bonus { get; set; } }

// Unrelated types used only to configure a circular IncludeBase pairing
file class CycleASrc { public string Name { get; set; } = ""; }
file class CycleADto { public string Name { get; set; } = ""; }
file class CycleBSrc { public string Name { get; set; } = ""; }
file class CycleBDto { public string Name { get; set; } = ""; }

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

    [Fact]
    public void IncludeBase_middle_level_Ignore_suppresses_mapping_from_more_distant_ancestor()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<GrandBaseSrc, GrandBaseDto>()
               .ForMember(d => d.Name, opts => opts.MapFrom(s => s.TheName));

            cfg.CreateMap<MidSrc, MidDto>()
               .ForMember(d => d.Name, opts => opts.Ignore())
               .IncludeBase<GrandBaseSrc, GrandBaseDto>();

            cfg.CreateMap<LeafSrc, LeafDto>().IncludeBase<MidSrc, MidDto>();
        }).CreateMapper();

        var src = new LeafSrc { TheName = "aaa", MidProp = 1, LeafProp = 2 };
        var dest = mapper.Map<LeafSrc, LeafDto>(src);

        dest.Name.Should().BeEmpty();
        dest.MidProp.Should().Be(1);
        dest.LeafProp.Should().Be(2);
    }

    [Fact]
    public void IncludeBase_base_level_mapping_runs_before_derived_condition_that_depends_on_it()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderBaseSrc, OrderBaseDto>()
               .ForMember(d => d.Status, opts => opts.MapFrom(s => s.Status));

            cfg.CreateMap<OrderDerivedSrc, OrderDerivedDto>()
               .ForMember(d => d.Bonus, opts =>
               {
                   opts.MapFrom(s => s.Bonus);
                   opts.Condition((src, dest) => dest.Status == "Active");
               })
               .IncludeBase<OrderBaseSrc, OrderBaseDto>();
        }).CreateMapper();

        var src = new OrderDerivedSrc { Status = "Active", Bonus = 50 };
        var dest = mapper.Map<OrderDerivedSrc, OrderDerivedDto>(src);

        dest.Status.Should().Be("Active");
        dest.Bonus.Should().Be(50);
    }

    [Fact]
    public void IncludeBase_circular_reference_throws_at_configuration_time()
    {
        Action act = () => new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CycleASrc, CycleADto>().IncludeBase<CycleBSrc, CycleBDto>();
            cfg.CreateMap<CycleBSrc, CycleBDto>().IncludeBase<CycleASrc, CycleADto>();
        });

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Circular IncludeBase*");
    }
}

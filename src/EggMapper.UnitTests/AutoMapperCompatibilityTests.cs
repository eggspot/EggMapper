using EggMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

#region Value resolver pattern
file class FakeAsset { public string Path { get; set; } = ""; }
file class FakeAssetService { public string? GetUrl(FakeAsset? a) => a == null ? null : "/cdn/" + a.Path; }

file class AssetUrlResolver : IMemberValueResolver<object, object, FakeAsset?, string?>
{
    private readonly FakeAssetService _svc;
    public AssetUrlResolver(FakeAssetService svc) { _svc = svc; }
    public string? Resolve(object source, object destination, FakeAsset? sourceMember, string? destMember, ResolutionContext context)
        => _svc.GetUrl(sourceMember);
}

file class AssetSrc { public FakeAsset? Image { get; set; } }
file class AssetDest { public string? ImageUrl { get; set; } }
#endregion

#region Two-param MapFrom
file class TpSrc { public string First { get; set; } = ""; public string Last { get; set; } = ""; }
file class TpDest { public string FullName { get; set; } = ""; }
#endregion

#region PreCondition
file class PreCondSrc { public string? Optional { get; set; } public int Other { get; set; } }
file class PreCondDest { public string Optional { get; set; } = "default"; public int Other { get; set; } }
#endregion

#region AfterMap with context.Mapper
file class AmInner { public int Id { get; set; } }
file class AmInnerVm { public int Id { get; set; } }
file class AmSrc { public List<AmInner> Items { get; set; } = new(); }
file class AmDest { public List<AmInnerVm> Items { get; set; } = new(); }
#endregion

#region Null collection source
file class NcSrc { public List<int>? SelectedIds { get; set; } }
file class NcDest { public List<int>? SelectedIds { get; set; } }
#endregion

#region Unmatched collection property
file class UmSrc { public string Name { get; set; } = ""; }
file class UmDest { public string Name { get; set; } = ""; public List<int>? UnmatchedIds { get; set; } }
#endregion

#region Null exclusion sibling types (string, dictionary)
file class NullStringSrc { public string? Name { get; set; } }
file class NullStringDest { public string? Name { get; set; } }

file class NullDictSrc { public Dictionary<string, int>? Map { get; set; } }
file class NullDictDest { public Dictionary<string, int>? Map { get; set; } }
#endregion

public class AutoMapperCompatibilityTests
{
    [Fact]
    public void ValueResolver_with_DI_resolves_via_service()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FakeAssetService>();
        services.AddEggMapper(cfg =>
        {
            cfg.CreateMap<AssetSrc, AssetDest>()
               .ForMember(d => d.ImageUrl, o => o.MapFrom<AssetUrlResolver, FakeAsset?>(s => s.Image));
        });
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var dest = mapper.Map<AssetSrc, AssetDest>(new AssetSrc { Image = new FakeAsset { Path = "logo.png" } });

        dest.ImageUrl.Should().Be("/cdn/logo.png");
    }

    [Fact]
    public void ValueResolver_null_source_member_returns_null()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FakeAssetService>();
        services.AddEggMapper(cfg =>
        {
            cfg.CreateMap<AssetSrc, AssetDest>()
               .ForMember(d => d.ImageUrl, o => o.MapFrom<AssetUrlResolver, FakeAsset?>(s => s.Image));
        });
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var dest = mapper.Map<AssetSrc, AssetDest>(new AssetSrc { Image = null });

        dest.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void TwoParam_MapFrom_with_source_and_destination()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<TpSrc, TpDest>()
               .ForMember(d => d.FullName, o => o.MapFrom((s, d) => s.First + " " + s.Last));
        }).CreateMapper();

        var dest = mapper.Map<TpSrc, TpDest>(new TpSrc { First = "Hai", Last = "Vo" });

        dest.FullName.Should().Be("Hai Vo");
    }

    [Fact]
    public void PreCondition_skips_member_when_false()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PreCondSrc, PreCondDest>()
               .ForMember(d => d.Optional, o =>
               {
                   o.PreCondition(s => s.Optional != null);
                   o.MapFrom(s => s.Optional!);
               });
        }).CreateMapper();

        var dest = mapper.Map<PreCondSrc, PreCondDest>(new PreCondSrc { Optional = null, Other = 7 });

        dest.Optional.Should().Be("default");
        dest.Other.Should().Be(7);
    }

    [Fact]
    public void PreCondition_runs_member_when_true()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PreCondSrc, PreCondDest>()
               .ForMember(d => d.Optional, o =>
               {
                   o.PreCondition(s => s.Optional != null);
                   o.MapFrom(s => s.Optional!);
               });
        }).CreateMapper();

        var dest = mapper.Map<PreCondSrc, PreCondDest>(new PreCondSrc { Optional = "set", Other = 7 });

        dest.Optional.Should().Be("set");
    }

    [Fact]
    public void Null_source_collection_maps_to_empty_collection_via_MapFrom()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NcSrc, NcDest>()
               .ForMember(d => d.SelectedIds, o => o.MapFrom(s => s.SelectedIds));
        }).CreateMapper();

        var dest = mapper.Map<NcSrc, NcDest>(new NcSrc { SelectedIds = null });

        dest.SelectedIds.Should().NotBeNull();
        dest.SelectedIds.Should().BeEmpty();
    }

    [Fact]
    public void Null_source_collection_maps_to_empty_collection_by_convention()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NcSrc, NcDest>();
        }).CreateMapper();

        var dest = mapper.Map<NcSrc, NcDest>(new NcSrc { SelectedIds = null });

        dest.SelectedIds.Should().NotBeNull();
        dest.SelectedIds.Should().BeEmpty();
    }

    [Fact]
    public void Unmatched_collection_dest_property_is_initialized_to_empty()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<UmSrc, UmDest>();
        }).CreateMapper();

        var dest = mapper.Map<UmSrc, UmDest>(new UmSrc { Name = "Alice" });

        dest.Name.Should().Be("Alice");
        dest.UnmatchedIds.Should().NotBeNull();
        dest.UnmatchedIds.Should().BeEmpty();
    }

    [Fact]
    public void Null_source_string_stays_null()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NullStringSrc, NullStringDest>()
               .ForMember(d => d.Name, o => o.MapFrom(s => s.Name));
        }).CreateMapper();

        var dest = mapper.Map<NullStringSrc, NullStringDest>(new NullStringSrc { Name = null });

        dest.Name.Should().BeNull();
    }

    [Fact]
    public void Null_source_dictionary_stays_null()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NullDictSrc, NullDictDest>()
               .ForMember(d => d.Map, o => o.MapFrom(s => s.Map));
        }).CreateMapper();

        var dest = mapper.Map<NullDictSrc, NullDictDest>(new NullDictSrc { Map = null });

        dest.Map.Should().BeNull();
    }

    [Fact]
    public void AfterMap_with_context_mapper_invokes_nested_map()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AmInner, AmInnerVm>();
            cfg.CreateMap<AmSrc, AmDest>()
               .ForMember(d => d.Items, o => o.Ignore())
               .AfterMap((s, t, c) => t.Items = s.Items?.Select(c.Mapper.Map<AmInner, AmInnerVm>).ToList() ?? new());
        }).CreateMapper();

        var dest = mapper.Map<AmSrc, AmDest>(new AmSrc
        {
            Items = new() { new AmInner { Id = 1 }, new AmInner { Id = 2 } }
        });

        dest.Items.Should().HaveCount(2);
        dest.Items[0].Id.Should().Be(1);
        dest.Items[1].Id.Should().Be(2);
    }
}

using EggMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

// Mirrors PAM patterns that need AutoMapper-compatible behavior.

#region Value resolver pattern (PAM/MediaAssetUrlValueResolver)
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

#region Two-param MapFrom (PAM/DefaultMapperProfile.cs:431)
file class TpSrc { public string First { get; set; } = ""; public string Last { get; set; } = ""; }
file class TpDest { public string FullName { get; set; } = ""; }
#endregion

#region PreCondition (PAM/ContentMapperProfile.cs:82)
file class PreCondSrc { public string? Optional { get; set; } public int Other { get; set; } }
file class PreCondDest { public string Optional { get; set; } = "default"; public int Other { get; set; } }
#endregion

#region AfterMap with c.Mapper (PAM/BonusMapperProfile.cs:382)
file class AmInner { public int Id { get; set; } }
file class AmInnerVm { public int Id { get; set; } }
file class AmSrc { public List<AmInner> Items { get; set; } = new(); }
file class AmDest { public List<AmInnerVm> Items { get; set; } = new(); }
#endregion

public class PamPatternsCompatibilityTests
{
    [Fact]
    public void Pam_ValueResolver_with_DI_resolves_via_service()
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
    public void Pam_ValueResolver_null_source_member_returns_null()
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
    public void Pam_TwoParam_MapFrom_with_source_and_destination()
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
    public void Pam_PreCondition_skips_member_when_false()
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
    public void Pam_PreCondition_runs_member_when_true()
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
    public void Pam_AfterMap_with_context_mapper_invokes_nested_map()
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

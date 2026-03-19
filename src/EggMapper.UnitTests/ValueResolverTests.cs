using EggMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class ValueResolverTests
{
    [Fact]
    public void MapFrom_WithValueResolver_ResolvesValue()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IVrService>(new VrService("https://cdn.example.com"));
        services.AddEggMapper(cfg =>
        {
            cfg.CreateMap<VrSource, VrDest>()
               .ForMember(d => d.Url, o => o.MapFrom<VrUrlResolver, VrAsset?>(s => s.Asset));
        });

        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var src = new VrSource { Name = "Test", Asset = new VrAsset { Path = "/img/photo.jpg" } };
        var result = mapper.Map<VrSource, VrDest>(src);
        result.Name.Should().Be("Test");
        result.Url.Should().Be("https://cdn.example.com/img/photo.jpg");
    }

    [Fact]
    public void MapFrom_WithValueResolver_NullSourceMember()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IVrService>(new VrService("https://cdn.example.com"));
        services.AddEggMapper(cfg =>
        {
            cfg.CreateMap<VrSource, VrDest>()
               .ForMember(d => d.Url, o => o.MapFrom<VrUrlResolver, VrAsset?>(s => s.Asset));
        });

        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var src = new VrSource { Name = "NoAsset", Asset = null };
        var result = mapper.Map<VrSource, VrDest>(src);
        result.Url.Should().Be("");
    }

    [Fact]
    public void MapFrom_WithValueResolver_MultipleResolvers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IVrService>(new VrService("https://cdn.example.com"));
        services.AddEggMapper(cfg =>
        {
            cfg.CreateMap<VrSource2, VrDest2>()
               .ForMember(d => d.MainUrl, o => o.MapFrom<VrUrlResolver, VrAsset?>(s => s.MainAsset))
               .ForMember(d => d.ThumbUrl, o => o.MapFrom<VrPreviewResolver, VrAsset?>(s => s.ThumbAsset));
        });

        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var src = new VrSource2
        {
            MainAsset = new VrAsset { Path = "/main.jpg" },
            ThumbAsset = new VrAsset { Path = "/thumb.jpg" }
        };
        var result = mapper.Map<VrSource2, VrDest2>(src);
        result.MainUrl.Should().Be("https://cdn.example.com/main.jpg");
        result.ThumbUrl.Should().Be("https://cdn.example.com/thumb_preview.jpg");
    }
}

// ── Test infrastructure ─────────────────────────────────────────────────

file interface IVrService
{
    string GetUrl(VrAsset? asset);
    string GetPreviewUrl(VrAsset? asset);
}

file class VrService : IVrService
{
    private readonly string _baseUrl;
    public VrService(string baseUrl) => _baseUrl = baseUrl;
    public string GetUrl(VrAsset? asset) => asset == null ? "" : _baseUrl + asset.Path;
    public string GetPreviewUrl(VrAsset? asset) =>
        asset == null ? "" : _baseUrl + asset.Path.Replace(".jpg", "_preview.jpg");
}

file class VrAsset { public string Path { get; set; } = ""; }

file class VrSource { public string Name { get; set; } = ""; public VrAsset? Asset { get; set; } }
file class VrDest { public string Name { get; set; } = ""; public string Url { get; set; } = ""; }

file class VrSource2 { public VrAsset? MainAsset { get; set; } public VrAsset? ThumbAsset { get; set; } }
file class VrDest2 { public string MainUrl { get; set; } = ""; public string ThumbUrl { get; set; } = ""; }

// ── Value resolvers (like AutoMapper's IMemberValueResolver) ────────────

file class VrUrlResolver : IMemberValueResolver<object, object, VrAsset?, string>
{
    private readonly IVrService _service;
    public VrUrlResolver(IVrService service) => _service = service;

    public string Resolve(object source, object destination, VrAsset? sourceMember, string destMember, ResolutionContext context)
        => _service.GetUrl(sourceMember);
}

file class VrPreviewResolver : IMemberValueResolver<object, object, VrAsset?, string>
{
    private readonly IVrService _service;
    public VrPreviewResolver(IVrService service) => _service = service;

    public string Resolve(object source, object destination, VrAsset? sourceMember, string destMember, ResolutionContext context)
        => _service.GetPreviewUrl(sourceMember);
}

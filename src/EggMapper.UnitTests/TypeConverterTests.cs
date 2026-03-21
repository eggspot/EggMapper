using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class TypeConverterTests
{
    [Fact]
    public void ConvertUsing_TypeConverter_Class()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<TcSource, TcDest>()
               .ConvertUsing<TcCustomConverter>());
        var mapper = config.CreateMapper();

        var result = mapper.Map<TcSource, TcDest>(new TcSource { Value = 5 });
        result.Value.Should().Be(50); // converter multiplies by 10
    }

    [Fact]
    public void ConvertUsing_TypeConverter_Instance()
    {
        var converter = new TcCustomConverter();
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<TcSource, TcDest>()
               .ConvertUsing(converter));
        var mapper = config.CreateMapper();

        var result = mapper.Map<TcSource, TcDest>(new TcSource { Value = 3 });
        result.Value.Should().Be(30);
    }

    [Fact]
    public void ConvertUsing_WithResolutionContext()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<TcSource, TcDest>()
               .ConvertUsing((s, d, ctx) => new TcDest { Value = s.Value + 100 }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<TcSource, TcDest>(new TcSource { Value = 7 });
        result.Value.Should().Be(107);
    }

    [Fact]
    public void ConvertUsing_SimpleInlineConversion()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<int?, string>()
               .ConvertUsing(s => s.HasValue ? s.Value.ToString() : "N/A"));
        var mapper = config.CreateMapper();

        mapper.Map<int?, string>(42).Should().Be("42");
        mapper.Map<int?, string>(null).Should().Be("N/A");
    }
}

file class TcSource { public int Value { get; set; } }
file class TcDest { public int Value { get; set; } }

file class TcCustomConverter : ITypeConverter<TcSource, TcDest>
{
    public TcDest Convert(TcSource source, TcDest? destination, ResolutionContext context)
        => new TcDest { Value = source.Value * 10 };
}

// ── Global type converter tests ───────────────────────────────────────────────

public class GlobalTypeConverterTests
{
    [Fact]
    public void GlobalConverter_AppliedToAllMaps()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddTypeConverter<DateTime, DateTimeOffset>(dt => new DateTimeOffset(dt, TimeSpan.Zero));
            cfg.CreateMap<GtcOrder, GtcOrderDto>();
            cfg.CreateMap<GtcProduct, GtcProductDto>();
        });
        var mapper = config.CreateMapper();

        var now = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var order = mapper.Map<GtcOrder, GtcOrderDto>(new GtcOrder { Id = 1, CreatedAt = now });
        order.CreatedAt.Should().Be(new DateTimeOffset(now, TimeSpan.Zero));

        var product = mapper.Map<GtcProduct, GtcProductDto>(new GtcProduct { Name = "Widget", UpdatedAt = now });
        product.UpdatedAt.Should().Be(new DateTimeOffset(now, TimeSpan.Zero));
    }

    [Fact]
    public void GlobalConverter_GuidToString()
    {
        var id = Guid.NewGuid();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddTypeConverter<Guid, string>(g => g.ToString());
            cfg.CreateMap<GtcEntity, GtcEntityDto>();
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<GtcEntity, GtcEntityDto>(new GtcEntity { Id = id });
        result.Id.Should().Be(id.ToString());
    }

    [Fact]
    public void GlobalConverter_ForMemberOverridesGlobal()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddTypeConverter<DateTime, DateTimeOffset>(dt => new DateTimeOffset(dt, TimeSpan.Zero));
            cfg.CreateMap<GtcOrder, GtcOrderDto>()
               .ForMember(d => d.CreatedAt, o => o.MapFrom(s => new DateTimeOffset(s.CreatedAt).AddHours(1)));
        });
        var mapper = config.CreateMapper();

        var now = new DateTime(2024, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var result = mapper.Map<GtcOrder, GtcOrderDto>(new GtcOrder { Id = 1, CreatedAt = now });
        // ForMember adds 1 hour — overrides global converter
        result.CreatedAt.Hour.Should().Be(11);
    }

    [Fact]
    public void GlobalConverter_MultipleConverters_CorrectOneApplied()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddTypeConverter<int, string>(i => $"#{i}");
            cfg.AddTypeConverter<double, string>(d => $"{d:F2}");
            cfg.CreateMap<GtcMixed, GtcMixedDto>();
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<GtcMixed, GtcMixedDto>(new GtcMixed { Count = 42, Rate = 3.14 });
        result.Count.Should().Be("#42");
        result.Rate.Should().Be("3.14");
    }

    [Fact]
    public void GlobalConverter_AppliedInListMapping()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddTypeConverter<Guid, string>(g => g.ToString());
            cfg.CreateMap<GtcEntity, GtcEntityDto>();
        });
        var mapper = config.CreateMapper();

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var results = mapper.MapList<GtcEntity, GtcEntityDto>(
            new List<GtcEntity> { new() { Id = id1 }, new() { Id = id2 } });

        results[0].Id.Should().Be(id1.ToString());
        results[1].Id.Should().Be(id2.ToString());
    }
}

file class GtcOrder { public int Id { get; set; } public DateTime CreatedAt { get; set; } }
file class GtcOrderDto { public int Id { get; set; } public DateTimeOffset CreatedAt { get; set; } }
file class GtcProduct { public string Name { get; set; } = ""; public DateTime UpdatedAt { get; set; } }
file class GtcProductDto { public string Name { get; set; } = ""; public DateTimeOffset UpdatedAt { get; set; } }
file class GtcEntity { public Guid Id { get; set; } }
file class GtcEntityDto { public string Id { get; set; } = ""; }
file class GtcMixed { public int Count { get; set; } public double Rate { get; set; } }
file class GtcMixedDto { public string Count { get; set; } = ""; public string Rate { get; set; } = ""; }

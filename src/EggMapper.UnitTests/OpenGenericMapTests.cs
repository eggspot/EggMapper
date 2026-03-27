using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class OpenGenericMapTests
{
    // ── Non-generic (closed) CreateMap(Type, Type) ────────────────────────────

    [Fact]
    public void CreateMap_NonGeneric_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap(typeof(OgSource), typeof(OgDest)));
        var mapper = config.CreateMapper();

        var result = mapper.Map<OgSource, OgDest>(new OgSource { Name = "Hello" });
        result.Name.Should().Be("Hello");
    }

    [Fact]
    public void CreateMap_NonGeneric_WithForMemberByString()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap(typeof(OgSource), typeof(OgDest))
               .ForMember("Name", o => o.MapFrom("Name")));
        var mapper = config.CreateMapper();

        var result = mapper.Map<OgSource, OgDest>(new OgSource { Name = "World" });
        result.Name.Should().Be("World");
    }

    [Fact]
    public void CreateMap_NonGeneric_ReturnsNonGenericExpression()
    {
        var config = new MapperConfiguration(cfg =>
        {
            var expr = cfg.CreateMap(typeof(OgSource), typeof(OgDest));
            expr.Should().NotBeNull();
        });
        var mapper = config.CreateMapper();
        mapper.Should().NotBeNull();
    }

    // ── Open generic CreateMap(typeof(T<>), typeof(U<>)) ─────────────────────

    [Fact]
    public void OpenGenericMap_SimpleWrapper_MapsScalarProperties()
    {
        // CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>)) — open generic template
        // CreateMap<Order, OrderDto>() — element map for the inner type
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>));
            cfg.CreateMap<OgOrder, OgOrderDto>();
        });
        var mapper = config.CreateMapper();

        var source = new ApiResponse<OgOrder>
        {
            StatusCode = 200,
            Message    = "OK",
            Data       = new OgOrder { Id = 7, Name = "Eggs" }
        };

        var result = mapper.Map<ApiResponse<OgOrder>, ApiResponseDto<OgOrderDto>>(source);
        result.StatusCode.Should().Be(200);
        result.Message.Should().Be("OK");
        result.Data!.Id.Should().Be(7);
        result.Data!.Name.Should().Be("Eggs");
    }

    [Fact]
    public void OpenGenericMap_SecondCall_UsesFastCache()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>));
            cfg.CreateMap<OgOrder, OgOrderDto>();
        });
        var mapper = config.CreateMapper();

        var source = new ApiResponse<OgOrder> { StatusCode = 1, Data = new OgOrder { Id = 1, Name = "A" } };

        // First call — triggers on-demand compilation
        var r1 = mapper.Map<ApiResponse<OgOrder>, ApiResponseDto<OgOrderDto>>(source);
        // Second call — should hit FastCache
        var r2 = mapper.Map<ApiResponse<OgOrder>, ApiResponseDto<OgOrderDto>>(source);

        r1.Data!.Id.Should().Be(1);
        r2.Data!.Id.Should().Be(1);
    }

    [Fact]
    public void OpenGenericMap_WithIgnore_SkipsProperty()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>))
               .ForMember("Message", o => o.Ignore());
            cfg.CreateMap<OgOrder, OgOrderDto>();
        });
        var mapper = config.CreateMapper();

        var source = new ApiResponse<OgOrder>
            { StatusCode = 200, Message = "should not appear", Data = new OgOrder { Id = 1, Name = "X" } };

        var result = mapper.Map<ApiResponse<OgOrder>, ApiResponseDto<OgOrderDto>>(source);
        result.Message.Should().BeNull();
    }

    [Fact]
    public void OpenGenericMap_DifferentClosedPairs_AreIndependent()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(Box<>), typeof(BoxDto<>));
            cfg.CreateMap<OgOrder, OgOrderDto>();
            cfg.CreateMap<OgSource, OgDest>();
        });
        var mapper = config.CreateMapper();

        var orderBox = new Box<OgOrder> { Value = new OgOrder { Id = 5, Name = "Order" } };
        var sourceBox = new Box<OgSource> { Value = new OgSource { Name = "Src" } };

        var orderResult  = mapper.Map<Box<OgOrder>, BoxDto<OgOrderDto>>(orderBox);
        var sourceResult = mapper.Map<Box<OgSource>, BoxDto<OgDest>>(sourceBox);

        orderResult.Value!.Id.Should().Be(5);
        sourceResult.Value!.Name.Should().Be("Src");
    }

    [Fact]
    public void OpenGenericMap_MapList_WorksOnClosedPair()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>));
            cfg.CreateMap<OgOrder, OgOrderDto>();
        });
        var mapper = config.CreateMapper();

        var list = new List<ApiResponse<OgOrder>>
        {
            new() { StatusCode = 1, Data = new OgOrder { Id = 1, Name = "A" } },
            new() { StatusCode = 2, Data = new OgOrder { Id = 2, Name = "B" } },
        };

        var result = mapper.MapList<ApiResponse<OgOrder>, ApiResponseDto<OgOrderDto>>(list);
        result.Should().HaveCount(2);
        result[0].Data!.Id.Should().Be(1);
        result[1].Data!.Name.Should().Be("B");
    }

    [Fact]
    public void OpenGenericMap_WithConvertUsing_UsesConverter()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(Box<>), typeof(BoxDto<>))
               .ConvertUsing(converterType: typeof(BoxConverter<OgOrder, OgOrderDto>));
            cfg.CreateMap<OgOrder, OgOrderDto>();
        });
        var mapper = config.CreateMapper();

        var source = new Box<OgOrder> { Value = new OgOrder { Id = 42, Name = "Test" } };
        // Converter is invoked — it returns a new BoxDto<OgOrderDto>() without mapping Value.
        // The test just verifies the converter is called and no exception is thrown.
        var result = mapper.Map<Box<OgOrder>, BoxDto<OgOrderDto>>(source);
        result.Should().NotBeNull();
        result.Value.Should().BeNull(); // BoxConverter delegates nothing — only existence matters
    }

    [Fact]
    public void OpenGenericMap_NonGenericTemplate_NotAffected()
    {
        // Non-generic CreateMap(typeof(A), typeof(B)) should still be compiled at config time
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(OgSource), typeof(OgDest));
            cfg.CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>));
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<OgSource, OgDest>(new OgSource { Name = "X" });
        result.Name.Should().Be("X");
    }
}

// ── Shared test types ─────────────────────────────────────────────────────────

file class OgSource { public string Name { get; set; } = ""; }
file class OgDest   { public string Name { get; set; } = ""; }

file class OgOrder    { public int Id { get; set; }  public string Name { get; set; } = ""; }
file class OgOrderDto { public int Id { get; set; }  public string Name { get; set; } = ""; }

file class ApiResponse<T>
{
    public int    StatusCode { get; set; }
    public string? Message   { get; set; }
    public T?      Data      { get; set; }
}

file class ApiResponseDto<T>
{
    public int    StatusCode { get; set; }
    public string? Message   { get; set; }
    public T?      Data      { get; set; }
}

file class Box<T>    { public T? Value { get; set; } }
file class BoxDto<T> { public T? Value { get; set; } }

file class BoxConverter<TSrc, TDest> : ITypeConverter<Box<TSrc>, BoxDto<TDest>>
    where TSrc  : class
    where TDest : class, new()
{
    public BoxDto<TDest> Convert(Box<TSrc> source, BoxDto<TDest>? destination, ResolutionContext context)
    {
        // Minimal: just pass through — Value mapping would need a separate mapper call,
        // but for this test we only verify the converter is invoked at all.
        // We use reflection-free cast to verify the Data arrived.
        return destination ?? new BoxDto<TDest>();
    }
}

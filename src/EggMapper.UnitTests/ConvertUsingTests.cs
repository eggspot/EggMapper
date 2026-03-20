using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ConvertUsingTests
{
    [Fact]
    public void ConvertUsing_InlineLambda_ReplacesEntireMapping()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CuSource, CuDest>()
               .ConvertUsing(s => new CuDest { Value = s.Input * 2 }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<CuSource, CuDest>(new CuSource { Input = 21 });
        result.Value.Should().Be(42);
    }

    [Fact]
    public void ConvertUsing_WithDestParam_HasAccess()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CuSource, CuDest>()
               .ConvertUsing((s, d) => new CuDest { Value = s.Input + 10 }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<CuSource, CuDest>(new CuSource { Input = 5 });
        result.Value.Should().Be(15);
    }

    [Fact]
    public void ConvertUsing_OverridesAllForMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CuSource, CuDest>()
               .ForMember(d => d.Value, o => o.MapFrom(s => s.Input * 100))
               .ConvertUsing(s => new CuDest { Value = s.Input }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<CuSource, CuDest>(new CuSource { Input = 7 });
        result.Value.Should().Be(7); // ConvertUsing wins
    }

    [Fact]
    public void ConvertUsing_SimpleTypeConversion()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<int, string>()
               .ConvertUsing(s => s.ToString()));
        var mapper = config.CreateMapper();

        var result = mapper.Map<int, string>(42);
        result.Should().Be("42");
    }
}

file class CuSource { public int Input { get; set; } }
file class CuDest { public int Value { get; set; } }

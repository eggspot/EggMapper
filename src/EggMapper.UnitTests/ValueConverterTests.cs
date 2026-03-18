using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class ConvSource { public int IntProp { get; set; } public long LongProp { get; set; } public double DoubleProp { get; set; } public float FloatProp { get; set; } }
file class ConvDestLong { public long IntProp { get; set; } public int LongProp { get; set; } }
file class ConvDestDouble { public double IntProp { get; set; } public float DoubleProp { get; set; } }
file class StringConvSource { public int Number { get; set; } }
file class StringConvDest { public string? Number { get; set; } }

public class ValueConverterTests
{
    [Fact]
    public void Int_to_long_widening_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ConvSource, ConvDestLong>()).CreateMapper();

        var src = new ConvSource { IntProp = 100 };
        var dest = mapper.Map<ConvSource, ConvDestLong>(src);
        dest.IntProp.Should().Be(100L);
    }

    [Fact]
    public void Long_to_int_narrowing_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ConvSource, ConvDestLong>()).CreateMapper();

        var src = new ConvSource { LongProp = 42L };
        var dest = mapper.Map<ConvSource, ConvDestLong>(src);
        dest.LongProp.Should().Be(42);
    }

    [Fact]
    public void Int_to_double_widening_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ConvSource, ConvDestDouble>()).CreateMapper();

        var src = new ConvSource { IntProp = 7 };
        var dest = mapper.Map<ConvSource, ConvDestDouble>(src);
        dest.IntProp.Should().Be(7.0);
    }

    [Fact]
    public void Double_to_float_narrowing_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ConvSource, ConvDestDouble>()).CreateMapper();

        var src = new ConvSource { DoubleProp = 3.14 };
        var dest = mapper.Map<ConvSource, ConvDestDouble>(src);
        dest.DoubleProp.Should().BeApproximately(3.14f, 0.001f);
    }

    [Fact]
    public void Int_to_string_via_MapFrom()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<StringConvSource, StringConvDest>()
               .ForMember(d => d.Number, opts => opts.MapFrom(s => s.Number.ToString()));
        }).CreateMapper();

        var src = new StringConvSource { Number = 42 };
        var dest = mapper.Map<StringConvSource, StringConvDest>(src);
        dest.Number.Should().Be("42");
    }

    [Fact]
    public void Zero_value_converts_correctly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ConvSource, ConvDestLong>()).CreateMapper();

        var src = new ConvSource { IntProp = 0 };
        var dest = mapper.Map<ConvSource, ConvDestLong>(src);
        dest.IntProp.Should().Be(0L);
    }

    [Fact]
    public void Negative_value_converts_correctly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ConvSource, ConvDestLong>()).CreateMapper();

        var src = new ConvSource { IntProp = -99 };
        var dest = mapper.Map<ConvSource, ConvDestLong>(src);
        dest.IntProp.Should().Be(-99L);
    }
}

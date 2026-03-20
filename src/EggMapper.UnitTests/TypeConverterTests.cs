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

using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class StringConversionTests
{
    [Fact]
    public void Map_IntToString_UsesCustomResolver()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntSource, StringDest>()
               .ForMember(d => d.Value, o => o.MapFrom((s, _) => s.Value.ToString())));
        var mapper = config.CreateMapper();

        var result = mapper.Map<IntSource, StringDest>(new IntSource { Value = 42 });
        result.Value.Should().Be("42");
    }

    [Fact]
    public void Map_StringToInt_UsesCustomResolver()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<StringSource, IntDest>()
               .ForMember(d => d.Value, o => o.MapFrom((s, _) => int.Parse(s.Value))));
        var mapper = config.CreateMapper();

        var result = mapper.Map<StringSource, IntDest>(new StringSource { Value = "42" });
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Map_DateTimeToString_UsesCustomResolver()
    {
        var dt = new DateTime(2024, 1, 15);
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<DateSource, StringDest>()
               .ForMember(d => d.Value, o => o.MapFrom((s, _) => s.Date.ToString("yyyy-MM-dd"))));
        var mapper = config.CreateMapper();

        var result = mapper.Map<DateSource, StringDest>(new DateSource { Date = dt });
        result.Value.Should().Be("2024-01-15");
    }

    [Fact]
    public void Map_EnumToString_UsesCustomResolver()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<EnumSource, StringDest>()
               .ForMember(d => d.Value, o => o.MapFrom((s, _) => s.Status.ToString())));
        var mapper = config.CreateMapper();

        var result = mapper.Map<EnumSource, StringDest>(new EnumSource { Status = Status.Active });
        result.Value.Should().Be("Active");
    }
}

file class IntSource { public int Value { get; set; } }
file class StringDest { public string Value { get; set; } = ""; }
file class StringSource { public string Value { get; set; } = ""; }
file class IntDest { public int Value { get; set; } }
file class DateSource { public DateTime Date { get; set; } }
file class EnumSource { public Status Status { get; set; } }
file enum Status { Inactive, Active, Suspended }

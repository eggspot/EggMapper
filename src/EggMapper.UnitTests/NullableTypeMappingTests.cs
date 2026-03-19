using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class NullableTypeMappingTests
{
    [Fact]
    public void NullableInt_ToInt_MapsValue()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableSource, NonNullableDest>()).CreateMapper();
        var src = new NullableSource { IntVal = 42 };
        var dest = mapper.Map<NullableSource, NonNullableDest>(src);
        dest.IntVal.Should().Be(42);
    }

    [Fact]
    public void NullableInt_Null_ToInt_MapsDefault()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableSource, NonNullableDest>()).CreateMapper();
        var src = new NullableSource { IntVal = null };
        var dest = mapper.Map<NullableSource, NonNullableDest>(src);
        dest.IntVal.Should().Be(0);
    }

    [Fact]
    public void Int_ToNullableInt_MapsValue()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NonNullableDest, NullableSource>()).CreateMapper();
        var src = new NonNullableDest { IntVal = 99 };
        var dest = mapper.Map<NonNullableDest, NullableSource>(src);
        dest.IntVal.Should().Be(99);
    }

    [Fact]
    public void NullableBool_ToNonNullable_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableSource, NonNullableDest>()).CreateMapper();

        mapper.Map<NullableSource, NonNullableDest>(new NullableSource { BoolVal = true })
            .BoolVal.Should().BeTrue();
        mapper.Map<NullableSource, NonNullableDest>(new NullableSource { BoolVal = null })
            .BoolVal.Should().BeFalse();
    }

    [Fact]
    public void NullableDouble_ToNonNullable_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableSource, NonNullableDest>()).CreateMapper();
        var src = new NullableSource { DoubleVal = 3.14 };
        var dest = mapper.Map<NullableSource, NonNullableDest>(src);
        dest.DoubleVal.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void MapList_NullableToNonNullable_MapsAllItems()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableSource, NonNullableDest>()).CreateMapper();

        var sources = new List<NullableSource>
        {
            new() { IntVal = 1, BoolVal = true, DoubleVal = 1.0 },
            new() { IntVal = null, BoolVal = null, DoubleVal = null },
            new() { IntVal = 3, BoolVal = false, DoubleVal = 3.0 }
        };

        var results = mapper.MapList<NullableSource, NonNullableDest>(sources);
        results.Should().HaveCount(3);
        results[0].IntVal.Should().Be(1);
        results[1].IntVal.Should().Be(0);
        results[2].IntVal.Should().Be(3);
    }
}

public class NullableSource
{
    public int? IntVal { get; set; }
    public bool? BoolVal { get; set; }
    public double? DoubleVal { get; set; }
}

public class NonNullableDest
{
    public int IntVal { get; set; }
    public bool BoolVal { get; set; }
    public double DoubleVal { get; set; }
}

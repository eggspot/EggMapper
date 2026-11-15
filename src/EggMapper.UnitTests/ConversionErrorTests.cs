using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ConversionErrorTests
{
    public class Text { public string Value { get; set; } = ""; }
    public class Number { public int Value { get; set; } }
    public class OptionalNumber { public int? Value { get; set; } }

    public class Inner { public string Name { get; set; } = ""; }
    public class InnerDto { public string Name { get; set; } = ""; }
    public class Outer { public Inner Inner { get; set; } = new(); }
    public class OuterDto { public InnerDto? Inner { get; set; } }

    private static IMapper MapperFor<TSource, TDestination>() =>
        new MapperConfiguration(cfg => cfg.CreateMap<TSource, TDestination>()).CreateMapper();

    [Fact]
    public void Map_TextThatIsNotANumberIntoInt_ThrowsNamingTheValueTheMemberAndTheFix()
    {
        var mapper = MapperFor<Text, Number>();

        var act = () => mapper.Map<Text, Number>(new Text { Value = "abc" });

        var exception = act.Should().Throw<MappingException>().Which;
        exception.Message.Should().Be(
            "Error mapping Text -> Number, member 'Value': Cannot convert String value 'abc' to Int32. " +
            "Map the member with .ForMember(d => d.X, o => o.MapFrom(s => ...)) or register cfg.AddTypeConverter<String, Int32>(...).");
        exception.InnerException.Should().BeOfType<InvalidCastException>()
            .Which.InnerException.Should().BeOfType<FormatException>();
    }

    [Fact]
    public void Map_EmptyTextIntoInt_ThrowsConversionErrorInsteadOfNullReference()
    {
        var mapper = MapperFor<Text, Number>();

        var act = () => mapper.Map<Text, Number>(new Text { Value = "" });

        act.Should().Throw<MappingException>().WithInnerException<InvalidCastException>();
    }

    [Fact]
    public void Map_NumericTextIntoInt_Converts()
    {
        MapperFor<Text, Number>().Map<Text, Number>(new Text { Value = "123" }).Value.Should().Be(123);
    }

    [Fact]
    public void Map_TextThatIsNotANumberIntoNullableInt_LeavesNull()
    {
        MapperFor<Text, OptionalNumber>().Map<Text, OptionalNumber>(new Text { Value = "abc" }).Value.Should().BeNull();
    }

    [Fact]
    public void Map_NestedObjectOfDifferentTypeWithoutItsOwnMap_LeavesNull()
    {
        MapperFor<Outer, OuterDto>().Map<Outer, OuterDto>(new Outer()).Inner.Should().BeNull();
    }
}

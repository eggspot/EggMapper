using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class MappingExceptionTests
{
    [Fact]
    public void Map_UnregisteredType_ThrowsInvalidOperationException()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var act = () => mapper.Map<UnregisteredSrc, UnregisteredDst>(new UnregisteredSrc());
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*No mapping configured*");
    }

    [Fact]
    public void MapList_UnregisteredType_ThrowsInvalidOperationException()
    {
        var config = new MapperConfiguration(cfg => { });
        var mapper = config.CreateMapper();

        var act = () => mapper.MapList<UnregisteredSrc, UnregisteredDst>(new List<UnregisteredSrc>());
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*No mapping configured*");
    }

    [Fact]
    public void Map_NullSource_ReturnsDefault()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<UnregisteredSrc, UnregisteredDst>());
        var mapper = config.CreateMapper();

        var result = mapper.Map<UnregisteredSrc, UnregisteredDst>(null!);
        result.Should().BeNull();
    }

    [Fact]
    public void MapList_NullSource_ThrowsArgumentNullException()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<UnregisteredSrc, UnregisteredDst>());
        var mapper = config.CreateMapper();

        var act = () => mapper.MapList<UnregisteredSrc, UnregisteredDst>(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

file class UnregisteredSrc { public string Name { get; set; } = ""; }
file class UnregisteredDst { public string Name { get; set; } = ""; }

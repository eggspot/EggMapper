using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Tests mapping a type to itself — important for deep clone scenarios.
/// </summary>
public class SameTypeMapTests
{
    [Fact]
    public void Map_SameType_CreatesDeepCopy()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CloneSource, CloneSource>());
        var mapper = config.CreateMapper();

        var src = new CloneSource { Name = "Original", Value = 42 };
        var dest = mapper.Map<CloneSource, CloneSource>(src);

        dest.Should().NotBeSameAs(src);
        dest.Name.Should().Be("Original");
        dest.Value.Should().Be(42);
    }

    [Fact]
    public void Map_SameTypeWithNested_CreatesDeepCopy()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<NestedClone, NestedClone>()
               .ForMember(d => d.Inner, o => o.Ignore()));
        var mapper = config.CreateMapper();

        var src = new NestedClone
        {
            Name = "Parent",
            Inner = new NestedClone { Name = "Child" }
        };

        var dest = mapper.Map<NestedClone, NestedClone>(src);
        dest.Name.Should().Be("Parent");
        dest.Inner.Should().BeNull(); // Ignored
    }

    [Fact]
    public void MapList_SameType_CreatesListOfCopies()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CloneSource, CloneSource>());
        var mapper = config.CreateMapper();

        var src = new List<CloneSource>
        {
            new() { Name = "A", Value = 1 },
            new() { Name = "B", Value = 2 }
        };

        var result = mapper.MapList<CloneSource, CloneSource>(src);
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("A");
        result[1].Name.Should().Be("B");
        result[0].Should().NotBeSameAs(src[0]);
    }
}

file class CloneSource
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

file class NestedClone
{
    public string Name { get; set; } = "";
    public NestedClone? Inner { get; set; }
}

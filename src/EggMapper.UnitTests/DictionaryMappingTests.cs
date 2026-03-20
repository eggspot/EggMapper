using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class DictionaryMappingTests
{
    // ── Models ────────────────────────────────────────────────────────────────

    class SrcWithDict
    {
        public Dictionary<string, int> Scores { get; set; } = new();
        public Dictionary<string, string> Labels { get; set; } = new();
    }

    class DestWithDict
    {
        public Dictionary<string, int> Scores { get; set; } = new();
        public Dictionary<string, string> Labels { get; set; } = new();
    }

    class SrcWithMappedValueDict
    {
        public Dictionary<string, InnerSrc> Items { get; set; } = new();
    }

    class DestWithMappedValueDict
    {
        public Dictionary<string, InnerDest> Items { get; set; } = new();
    }

    class InnerSrc  { public string Name { get; set; } = ""; }
    class InnerDest { public string Name { get; set; } = ""; }

    class SrcStandalone
    {
        public string Name { get; set; } = "";
        public Dictionary<string, int> Meta { get; set; } = new();
    }

    class DestStandalone
    {
        public string Name { get; set; } = "";
        public Dictionary<string, int> Meta { get; set; } = new();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Map_SameType_Dictionary_Copies_Entries()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcWithDict, DestWithDict>()).CreateMapper();

        var src = new SrcWithDict
        {
            Scores = new Dictionary<string, int> { ["Alice"] = 95, ["Bob"] = 80 },
            Labels = new Dictionary<string, string> { ["x"] = "foo" }
        };

        var dest = mapper.Map<SrcWithDict, DestWithDict>(src);

        dest.Scores.Should().BeEquivalentTo(src.Scores);
        dest.Labels.Should().BeEquivalentTo(src.Labels);
    }

    [Fact]
    public void Map_Dictionary_Is_A_Copy_Not_Same_Reference()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcWithDict, DestWithDict>()).CreateMapper();

        var src = new SrcWithDict { Scores = new Dictionary<string, int> { ["k"] = 1 } };
        var dest = mapper.Map<SrcWithDict, DestWithDict>(src);

        dest.Scores.Should().NotBeSameAs(src.Scores);
        dest.Scores["k"].Should().Be(1);
    }

    [Fact]
    public void Map_Null_Dictionary_Property_Stays_Default()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcWithDict, DestWithDict>()).CreateMapper();

        var src = new SrcWithDict { Scores = null! };
        var dest = mapper.Map<SrcWithDict, DestWithDict>(src);

        // null source dict → dest prop not set (stays at its initializer value or null)
        dest.Scores.Should().BeEquivalentTo(new Dictionary<string, int>());
    }

    [Fact]
    public void Map_Dictionary_With_Mapped_Values_Maps_Each_Value()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<InnerSrc, InnerDest>();
            cfg.CreateMap<SrcWithMappedValueDict, DestWithMappedValueDict>();
        }).CreateMapper();

        var src = new SrcWithMappedValueDict
        {
            Items = new Dictionary<string, InnerSrc>
            {
                ["a"] = new InnerSrc { Name = "Alpha" },
                ["b"] = new InnerSrc { Name = "Beta" }
            }
        };

        var dest = mapper.Map<SrcWithMappedValueDict, DestWithMappedValueDict>(src);

        dest.Items.Should().HaveCount(2);
        dest.Items["a"].Name.Should().Be("Alpha");
        dest.Items["b"].Name.Should().Be("Beta");
        dest.Items["a"].Should().NotBeSameAs(src.Items["a"]);
    }

    [Fact]
    public void Map_Empty_Dictionary_Produces_Empty_Destination()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcWithDict, DestWithDict>()).CreateMapper();

        var src = new SrcWithDict { Scores = new Dictionary<string, int>() };
        var dest = mapper.Map<SrcWithDict, DestWithDict>(src);

        dest.Scores.Should().BeEmpty();
    }

    [Fact]
    public void MapList_With_Dictionary_Property_Maps_All_Elements()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcStandalone, DestStandalone>()).CreateMapper();

        var sources = new List<SrcStandalone>
        {
            new() { Name = "A", Meta = new() { ["x"] = 1 } },
            new() { Name = "B", Meta = new() { ["y"] = 2 } }
        };

        var result = mapper.MapList<SrcStandalone, DestStandalone>(sources);

        result.Should().HaveCount(2);
        result[0].Meta["x"].Should().Be(1);
        result[1].Meta["y"].Should().Be(2);
    }
}

using System.Collections;
using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class WrapperCollection : IEnumerable<string>
{
    private readonly List<string> _items;
    public WrapperCollection(IEnumerable items) => _items = items.Cast<string>().ToList();
    public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

file class SrcWithList { public List<string> Items { get; set; } = []; }
file class DestWithWrapper { public WrapperCollection Items { get; set; } = new(Array.Empty<string>()); }

file class SrcWithNested { public string Value { get; set; } = ""; }
file class DestWithNested { public string Value { get; set; } = ""; }
file class SrcWithNestedProp { public SrcWithNested? Nested { get; set; } }
file class DestWithSameTypeProp { public DestWithNested? Nested { get; set; } }

public class MapOrConvertTests
{
    [Fact]
    public void MapOrConvert_constructs_custom_collection_via_IEnumerable_ctor()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcWithList, DestWithWrapper>()).CreateMapper();

        var dest = mapper.Map<SrcWithList, DestWithWrapper>(
            new SrcWithList { Items = ["a", "b"] });

        dest.Items.Should().BeEquivalentTo(new[] { "a", "b" });
    }

    [Fact]
    public void MapOrConvert_custom_collection_null_source_maps_to_null()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<SrcWithList, DestWithWrapper>()).CreateMapper();

        var dest = mapper.Map<SrcWithList, DestWithWrapper>(new SrcWithList { Items = null! });

        dest.Items.Should().BeNull();
    }

    [Fact]
    public void MapOrConvert_does_not_corrupt_correctly_typed_registered_map()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SrcWithNested, DestWithNested>();
            cfg.CreateMap<SrcWithNestedProp, DestWithSameTypeProp>();
        }).CreateMapper();

        var dest = mapper.Map<SrcWithNestedProp, DestWithSameTypeProp>(
            new SrcWithNestedProp { Nested = new SrcWithNested { Value = "hello" } });

        dest.Nested!.Value.Should().Be("hello");
    }
}

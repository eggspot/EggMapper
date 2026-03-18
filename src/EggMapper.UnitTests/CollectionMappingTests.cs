using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

// Models only used in this test class
file class IntListSource { public List<int>? Numbers { get; set; } }
file class IntHashSetDest { public HashSet<int>? Numbers { get; set; } }
file class IntICollectionDest { public ICollection<int>? Numbers { get; set; } }
file class IntIReadOnlyListDest { public IReadOnlyList<int>? Numbers { get; set; } }
file class NestedCollSource
{
    public List<ItemSource>? Items { get; set; }
    public List<List<int>>? Matrix { get; set; }
}
file class NestedCollDest
{
    public List<ItemDest>? Items { get; set; }
}

public class CollectionMappingTests
{
    [Fact]
    public void Maps_list_to_hashset()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntListSource, IntHashSetDest>()).CreateMapper();

        var src = new IntListSource { Numbers = new List<int> { 1, 2, 3, 2 } };
        var dest = mapper.Map<IntListSource, IntHashSetDest>(src);
        dest.Numbers.Should().NotBeNull();
        dest.Numbers.Should().BeOfType<HashSet<int>>();
        dest.Numbers!.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    [Fact]
    public void Maps_list_to_ICollection()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntListSource, IntICollectionDest>()).CreateMapper();

        var src = new IntListSource { Numbers = new List<int> { 10, 20 } };
        var dest = mapper.Map<IntListSource, IntICollectionDest>(src);
        dest.Numbers.Should().NotBeNull();
        dest.Numbers.Should().HaveCount(2);
        dest.Numbers.Should().Contain(10).And.Contain(20);
    }

    [Fact]
    public void Maps_list_to_IReadOnlyList()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntListSource, IntIReadOnlyListDest>()).CreateMapper();

        var src = new IntListSource { Numbers = new List<int> { 5, 6, 7 } };
        var dest = mapper.Map<IntListSource, IntIReadOnlyListDest>(src);
        dest.Numbers.Should().NotBeNull();
        dest.Numbers.Should().HaveCount(3);
        dest.Numbers![1].Should().Be(6);
    }

    [Fact]
    public void Null_collection_maps_to_null()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntListSource, IntHashSetDest>()).CreateMapper();

        var src = new IntListSource { Numbers = null };
        var dest = mapper.Map<IntListSource, IntHashSetDest>(src);
        dest.Numbers.Should().BeNull();
    }

    [Fact]
    public void Replaces_pre_existing_destination_collection()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ItemSource, ItemDest>();
            cfg.CreateMap<CollectionSource, CollectionDest>();
        }).CreateMapper();

        var src = new CollectionSource
        {
            Items = new List<ItemSource> { new() { Id = 99, Label = "New" } }
        };
        var existingDest = new CollectionDest
        {
            Items = new List<ItemDest> { new() { Id = 1, Label = "Old" } }
        };

        var dest = mapper.Map<CollectionSource, CollectionDest>(src, existingDest);
        dest.Items.Should().HaveCount(1);
        dest.Items![0].Id.Should().Be(99);
    }

    [Fact]
    public void Maps_nested_object_collection()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ItemSource, ItemDest>();
            cfg.CreateMap<NestedCollSource, NestedCollDest>();
        }).CreateMapper();

        var src = new NestedCollSource
        {
            Items = new List<ItemSource>
            {
                new() { Id = 1, Label = "One" },
                new() { Id = 2, Label = "Two" }
            }
        };
        var dest = mapper.Map<NestedCollSource, NestedCollDest>(src);
        dest.Items.Should().HaveCount(2);
        dest.Items![0].Label.Should().Be("One");
    }

    [Fact]
    public void Empty_hashset_dest_stays_empty()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntListSource, IntHashSetDest>()).CreateMapper();

        var src = new IntListSource { Numbers = new List<int>() };
        var dest = mapper.Map<IntListSource, IntHashSetDest>(src);
        dest.Numbers.Should().BeEmpty();
    }
}

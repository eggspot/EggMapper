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
    public void Null_collection_maps_to_empty()
    {
        // Null source collection maps to empty destination collection (default AllowNullCollections=false)
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<IntListSource, IntHashSetDest>()).CreateMapper();

        var src = new IntListSource { Numbers = null };
        var dest = mapper.Map<IntListSource, IntHashSetDest>(src);
        dest.Numbers.Should().NotBeNull().And.BeEmpty();
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

    // ── Collection auto-mapping (Map<IList<T>>, Map<IEnumerable<T>>) ─────────
    // Collection auto-mapping: mapper.Map<IList<TDest>>(List<TSrc>) routes to
    // the registered element map TSrc→TDest and returns a List<TDest>.

    [Fact]
    public void Map_IList_dest_uses_element_map()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        var src = new List<ItemSource> { new() { Id = 1, Label = "A" }, new() { Id = 2, Label = "B" } };
        var dest = mapper.Map<IList<ItemDest>>(src);

        dest.Should().HaveCount(2);
        dest[0].Id.Should().Be(1);
        dest[1].Label.Should().Be("B");
    }

    [Fact]
    public void Map_IEnumerable_dest_uses_element_map()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        var src = new List<ItemSource> { new() { Id = 7, Label = "X" } };
        var dest = mapper.Map<IEnumerable<ItemDest>>(src);

        dest.Should().ContainSingle(d => d.Id == 7 && d.Label == "X");
    }

    [Fact]
    public void Map_IList_dest_with_AfterMap_opts_runs_callback()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        var src = new List<ItemSource> { new() { Id = 3, Label = "C" } };
        var callbackRan = false;

        var dest = mapper.Map<IList<ItemDest>>(src,
            opt => opt.AfterMap((s, d) =>
            {
                callbackRan = true;
                d[0].Label = "patched";
            }));

        callbackRan.Should().BeTrue();
        dest[0].Label.Should().Be("patched");
    }

    [Fact]
    public void Map_ListDest_from_object_source_uses_element_map()
    {
        // Exact pattern: mapper.Map<List<TDest>>(listOfSrc) — no explicit collection map registered
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        object src = new List<ItemSource> { new() { Id = 10, Label = "Z" }, new() { Id = 20, Label = "Y" } };
        var dest = mapper.Map<List<ItemDest>>(src);

        dest.Should().HaveCount(2);
        dest[0].Id.Should().Be(10);
        dest[1].Label.Should().Be("Y");
    }

    [Fact]
    public void Map_TwoTypeArg_ListToList_uses_element_map()
    {
        // Map<List<TSrc>, List<TDest>>(list) — two-type-arg generic overload
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        var src = new List<ItemSource> { new() { Id = 5, Label = "Q" } };
        var dest = mapper.Map<List<ItemSource>, List<ItemDest>>(src);

        dest.Should().ContainSingle(d => d.Id == 5 && d.Label == "Q");
    }

    [Fact]
    public void Map_Collection_with_null_elements_preserves_nulls()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        object src = new List<ItemSource> { new() { Id = 1 }, null!, new() { Id = 3 } };
        var dest = mapper.Map<List<ItemDest>>(src);

        dest.Should().HaveCount(3);
        dest[0].Id.Should().Be(1);
        dest[1].Should().BeNull();
        dest[2].Id.Should().Be(3);
    }

    [Fact]
    public void Map_Array_dest_from_object_source()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ItemSource, ItemDest>()).CreateMapper();

        object src = new ItemSource[] { new() { Id = 42, Label = "arr" } };
        var dest = mapper.Map<IEnumerable<ItemDest>>(src);

        dest.Should().ContainSingle(d => d.Id == 42 && d.Label == "arr");
    }
}

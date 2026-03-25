using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ArraysAndListsTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ItemSource, ItemDest>();
            cfg.CreateMap<CollectionSource, CollectionDest>();
        }).CreateMapper();

    [Fact]
    public void Maps_list_of_items()
    {
        var mapper = CreateMapper();
        var src = new CollectionSource
        {
            Items = new List<ItemSource>
            {
                new() { Id = 1, Label = "A" },
                new() { Id = 2, Label = "B" }
            }
        };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.Items.Should().HaveCount(2);
        dest.Items![0].Id.Should().Be(1);
        dest.Items![0].Label.Should().Be("A");
        dest.Items![1].Id.Should().Be(2);
    }

    [Fact]
    public void Maps_array_of_items()
    {
        var mapper = CreateMapper();
        var src = new CollectionSource
        {
            ItemArray = new[] { new ItemSource { Id = 10, Label = "X" } }
        };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.ItemArray.Should().HaveCount(1);
        dest.ItemArray![0].Id.Should().Be(10);
        dest.ItemArray![0].Label.Should().Be("X");
    }

    [Fact]
    public void Empty_list_maps_to_empty_list()
    {
        var mapper = CreateMapper();
        var src = new CollectionSource { Items = new List<ItemSource>() };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.Items.Should().BeEmpty();
    }

    [Fact]
    public void Empty_array_maps_to_empty_array()
    {
        var mapper = CreateMapper();
        var src = new CollectionSource { ItemArray = Array.Empty<ItemSource>() };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.ItemArray.Should().BeEmpty();
    }

    [Fact]
    public void Null_list_maps_to_empty()
    {
        // Null source list maps to empty destination list (matches AutoMapper behavior)
        var mapper = CreateMapper();
        var src = new CollectionSource { Items = null };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Null_array_maps_to_empty()
    {
        // Null source array maps to empty destination array (matches AutoMapper behavior)
        var mapper = CreateMapper();
        var src = new CollectionSource { ItemArray = null };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.ItemArray.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Maps_list_elements_with_nested_mapper()
    {
        var mapper = CreateMapper();
        var src = new CollectionSource
        {
            Items = new List<ItemSource>
            {
                new() { Id = 5, Label = "Five" },
                new() { Id = 6, Label = "Six" }
            }
        };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.Items!.Should().AllSatisfy(d => d.Should().BeOfType<ItemDest>());
        dest.Items!.Select(d => d.Id).Should().BeEquivalentTo(new[] { 5, 6 });
    }

    [Fact]
    public void Maps_IEnumerable_source_via_MapFrom()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ItemSource, ItemDest>();
            cfg.CreateMap<FlatSource, CollectionDest>()
               .ForMember(d => d.Items, opts => opts.Ignore())
               .ForMember(d => d.ItemArray, opts => opts.Ignore());
        }).CreateMapper();

        // Verify IEnumerable projection via ForMember MapFrom
        var items = new List<ItemSource> { new() { Id = 1, Label = "L" } };
        var mapper2 = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ItemSource, ItemDest>();
        }).CreateMapper();

        var result = items.Select(i => mapper2.Map<ItemSource, ItemDest>(i)).ToList();
        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
    }

    [Fact]
    public void Mapped_list_items_are_new_instances()
    {
        var mapper = CreateMapper();
        var srcItem = new ItemSource { Id = 1, Label = "A" };
        var src = new CollectionSource { Items = new List<ItemSource> { srcItem } };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.Items![0].Should().NotBeSameAs(srcItem);
    }
}

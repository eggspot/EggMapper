using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class BeforeAfterMappingTests
{
    [Fact]
    public void BeforeMap_fires_before_properties_are_set()
    {
        string? nameInBeforeMap = null;

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SimpleSource, SimpleDest>()
               .BeforeMap((src, dest) => nameInBeforeMap = dest.Value);
        }).CreateMapper();

        var src = new SimpleSource { Value = "Hello" };
        mapper.Map<SimpleSource, SimpleDest>(src);

        // Before mapping fires before Value is set, so dest.Value is still default ("")
        nameInBeforeMap.Should().BeEmpty();
    }

    [Fact]
    public void AfterMap_fires_after_properties_are_set()
    {
        string? nameInAfterMap = null;

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SimpleSource, SimpleDest>()
               .AfterMap((src, dest) => nameInAfterMap = dest.Value);
        }).CreateMapper();

        var src = new SimpleSource { Value = "World" };
        mapper.Map<SimpleSource, SimpleDest>(src);

        // After mapping fires after Value is set
        nameInAfterMap.Should().Be("World");
    }

    [Fact]
    public void AfterMap_can_modify_destination()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SimpleSource, SimpleDest>()
               .AfterMap((src, dest) => dest.Extra = "added-by-after");
        }).CreateMapper();

        var src = new SimpleSource { Value = "Test" };
        var dest = mapper.Map<SimpleSource, SimpleDest>(src);

        dest.Extra.Should().Be("added-by-after");
        dest.Value.Should().Be("Test");
    }

    [Fact]
    public void BeforeMap_and_AfterMap_both_fire_in_order()
    {
        var order = new List<string>();

        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SimpleSource, SimpleDest>()
               .BeforeMap((src, dest) => order.Add("before"))
               .AfterMap((src, dest) => order.Add("after"));
        }).CreateMapper();

        mapper.Map<SimpleSource, SimpleDest>(new SimpleSource { Value = "v" });

        order.Should().ContainInOrder("before", "after");
    }
}

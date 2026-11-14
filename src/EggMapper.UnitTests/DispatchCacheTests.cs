using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class DispatchCacheTests
{
    public class SameTypeItem
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    [Fact]
    public void MapIntoExisting_SameTypeWithoutCreateMap_CalledFirst_MapsIntoInstance()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
        var destination = new SameTypeItem();

        var result = mapper.Map(new SameTypeItem { Name = "a", Value = 1 }, destination);

        result.Should().BeSameAs(destination);
        destination.Name.Should().Be("a");
        destination.Value.Should().Be(1);
    }

    [Fact]
    public void MapIntoExisting_SameTypeWithoutCreateMap_AfterMapNew_MapsIntoInstance()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
        mapper.Map<SameTypeItem, SameTypeItem>(new SameTypeItem { Name = "warm" });
        var destination = new SameTypeItem();

        var result = mapper.Map(new SameTypeItem { Name = "b", Value = 2 }, destination);

        result.Should().BeSameAs(destination);
        destination.Name.Should().Be("b");
        destination.Value.Should().Be(2);
    }
}

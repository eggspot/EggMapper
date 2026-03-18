using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class LargeCollectionTests
{
    [Fact]
    public void MapList_1000Items_AllMappedCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>());
        var mapper = config.CreateMapper();

        var sources = Enumerable.Range(0, 1000).Select(i => new FlatSource
        {
            Name = $"Item{i}",
            Age = i,
            Value = i * 1.1,
            Email = $"user{i}@test.com",
            IsActive = i % 2 == 0
        }).ToList();

        var results = mapper.MapList<FlatSource, FlatDest>(sources);

        results.Should().HaveCount(1000);
        results[0].Name.Should().Be("Item0");
        results[999].Name.Should().Be("Item999");
        results[500].Age.Should().Be(500);
        results[250].IsActive.Should().BeTrue();
        results[251].IsActive.Should().BeFalse();
    }

    [Fact]
    public void MapList_10000Items_WithNestedObjects_AllMappedCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        });
        var mapper = config.CreateMapper();

        var sources = Enumerable.Range(0, 10000).Select(i => new PersonSource
        {
            Name = $"Person{i}",
            Age = i % 100,
            Address = new AddressSource
            {
                Street = $"Street{i}",
                City = $"City{i % 50}",
                Zip = $"{i:D5}"
            }
        }).ToList();

        var results = mapper.MapList<PersonSource, PersonDest>(sources);

        results.Should().HaveCount(10000);
        results[0].Address!.Street.Should().Be("Street0");
        results[9999].Name.Should().Be("Person9999");
        results[5000].Address!.City.Should().Be("City0");
    }

    [Fact]
    public void MapList_FromIEnumerable_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>());
        var mapper = config.CreateMapper();

        // Use IEnumerable (not IList) to test the non-indexed path
        IEnumerable<FlatSource> sources = Enumerable.Range(0, 500).Select(i => new FlatSource
        {
            Name = $"Enum{i}",
            Age = i
        });

        var results = mapper.MapList<FlatSource, FlatDest>(sources);

        results.Should().HaveCount(500);
        results[0].Name.Should().Be("Enum0");
        results[499].Name.Should().Be("Enum499");
    }
}

using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class MappingToExistingDestinationTests
{
    [Fact]
    public void Map_ToExistingDest_UpdatesProperties()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "Updated", Age = 99, Value = 1.5 };
        var existing = new FlatDest { Name = "Old", Age = 0, Email = "old@old.com" };

        var result = mapper.Map(src, existing);

        result.Should().BeSameAs(existing);
        result.Name.Should().Be("Updated");
        result.Age.Should().Be(99);
        result.Value.Should().Be(1.5);
        result.Email.Should().Be(""); // overwritten from source default
    }

    [Fact]
    public void Map_ToExistingDest_WithNestedObjects_UpdatesNested()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        }).CreateMapper();

        var src = new PersonSource
        {
            Name = "New Name",
            Age = 30,
            Address = new AddressSource { Street = "New Street", City = "New City", Zip = "12345" }
        };
        var existing = new PersonDest { Name = "Old", Age = 20 };

        var result = mapper.Map(src, existing);

        result.Should().BeSameAs(existing);
        result.Name.Should().Be("New Name");
        result.Age.Should().Be(30);
        result.Address.Should().NotBeNull();
        result.Address!.Street.Should().Be("New Street");
    }

    [Fact]
    public void Map_ToExistingDest_NullSource_ReturnsExistingDest()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var existing = new FlatDest { Name = "Preserved" };

        var result = mapper.Map<FlatSource, FlatDest>(null!, existing);

        result.Should().BeSameAs(existing);
        result.Name.Should().Be("Preserved");
    }
}

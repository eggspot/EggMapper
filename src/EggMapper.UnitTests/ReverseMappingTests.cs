using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ReverseMappingTests
{
    [Fact]
    public void ReverseMap_registers_reverse_configuration()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>().ReverseMap();
        });
        var mapper = config.CreateMapper();

        var src = new FlatSource { Name = "Alice", Age = 30 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Alice");
    }

    [Fact]
    public void ReverseMap_allows_mapping_dest_to_source()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>().ReverseMap();
        }).CreateMapper();

        var dest = new FlatDest { Name = "Bob", Age = 25, IsActive = true };
        var src = mapper.Map<FlatDest, FlatSource>(dest);
        src.Name.Should().Be("Bob");
        src.Age.Should().Be(25);
        src.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Forward_map_still_works_after_ReverseMap()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>().ReverseMap();
        }).CreateMapper();

        var src = new FlatSource { Name = "Carol", Age = 40 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Carol");
        dest.Age.Should().Be(40);
    }

    [Fact]
    public void ReverseMap_maps_all_matching_properties()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>().ReverseMap();
        }).CreateMapper();

        var dest = new FlatDest { Name = "Dan", Age = 50, Value = 2.5, Email = "d@d.com", IsActive = false };
        var src = mapper.Map<FlatDest, FlatSource>(dest);
        src.Name.Should().Be("Dan");
        src.Age.Should().Be(50);
        src.Value.Should().Be(2.5);
        src.Email.Should().Be("d@d.com");
        src.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ReverseMap_nested_works_when_both_directions_configured()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>().ReverseMap();
            cfg.CreateMap<PersonSource, PersonDest>().ReverseMap();
        }).CreateMapper();

        var dest = new PersonDest
        {
            Name = "Eve",
            Age = 28,
            Address = new AddressDest { Street = "Main St", City = "NYC", Zip = "10001" }
        };
        var src = mapper.Map<PersonDest, PersonSource>(dest);
        src.Name.Should().Be("Eve");
        src.Address.Should().NotBeNull();
        src.Address!.City.Should().Be("NYC");
    }
}

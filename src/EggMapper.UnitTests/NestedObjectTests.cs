using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class Level1 { public string Name { get; set; } = ""; public Level2? Child { get; set; } }
file class Level2 { public string Name { get; set; } = ""; public Level3? Child { get; set; } }
file class Level3 { public string Name { get; set; } = ""; }
file class Level1Dest { public string Name { get; set; } = ""; public Level2Dest? Child { get; set; } }
file class Level2Dest { public string Name { get; set; } = ""; public Level3Dest? Child { get; set; } }
file class Level3Dest { public string Name { get; set; } = ""; }

public class NestedObjectTests
{
    private static IMapper CreateNestedMapper() =>
        new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        }).CreateMapper();

    [Fact]
    public void Nested_object_is_mapped_deeply()
    {
        var mapper = CreateNestedMapper();
        var src = new PersonSource
        {
            Name = "Alice",
            Age = 30,
            Address = new AddressSource { Street = "123 Main", City = "Springfield", Zip = "62701" }
        };
        var dest = mapper.Map<PersonSource, PersonDest>(src);
        dest.Name.Should().Be("Alice");
        dest.Address.Should().NotBeNull();
        dest.Address!.Street.Should().Be("123 Main");
        dest.Address.City.Should().Be("Springfield");
        dest.Address.Zip.Should().Be("62701");
    }

    [Fact]
    public void Null_nested_object_results_in_null_dest_property()
    {
        var mapper = CreateNestedMapper();
        var src = new PersonSource { Name = "Bob", Address = null };
        var dest = mapper.Map<PersonSource, PersonDest>(src);
        dest.Address.Should().BeNull();
    }

    [Fact]
    public void Nested_object_is_a_new_instance()
    {
        var mapper = CreateNestedMapper();
        var addr = new AddressSource { Street = "1st Ave" };
        var src = new PersonSource { Name = "Carol", Address = addr };
        var dest = mapper.Map<PersonSource, PersonDest>(src);
        dest.Address.Should().NotBeSameAs(addr);
    }

    [Fact]
    public void Multi_level_nesting_maps_all_levels()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Level3, Level3Dest>();
            cfg.CreateMap<Level2, Level2Dest>();
            cfg.CreateMap<Level1, Level1Dest>();
        }).CreateMapper();

        var src = new Level1
        {
            Name = "L1",
            Child = new Level2
            {
                Name = "L2",
                Child = new Level3 { Name = "L3" }
            }
        };

        var dest = mapper.Map<Level1, Level1Dest>(src);
        dest.Name.Should().Be("L1");
        dest.Child!.Name.Should().Be("L2");
        dest.Child.Child!.Name.Should().Be("L3");
    }

    [Fact]
    public void Nested_object_properties_all_mapped()
    {
        var mapper = CreateNestedMapper();
        var src = new PersonSource
        {
            Name = "Dave",
            Age = 45,
            Address = new AddressSource { Street = "Oak St", City = "Boston", Zip = "02101" }
        };
        var dest = mapper.Map<PersonSource, PersonDest>(src);
        dest.Address!.Street.Should().Be("Oak St");
        dest.Address.City.Should().Be("Boston");
        dest.Address.Zip.Should().Be("02101");
    }
}

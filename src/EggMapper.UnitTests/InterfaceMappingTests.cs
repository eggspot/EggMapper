using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class InterfaceMappingTests
{
    [Fact]
    public void Concrete_source_assigned_to_interface_property_maps_by_reference()
    {
        // When dest property type == source property type (both IAnimal), direct assignment
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AnimalHolder, AnimalHolder>();
        }).CreateMapper();

        var cat = new Cat { Name = "Whiskers", Sound = "Meow" };
        var src = new AnimalHolder { Animal = cat };
        var dest = mapper.Map<AnimalHolder, AnimalHolder>(src);
        dest.Animal.Should().NotBeNull();
        dest.Animal!.Name.Should().Be("Whiskers");
    }

    [Fact]
    public void Interface_typed_source_property_with_nested_map()
    {
        // Source holds IAnimal (runtime Cat), dest holds AnimalDest
        // Need explicit map for the interface type pair
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Cat, AnimalDest>();
        }).CreateMapper();

        var cat = new Cat { Name = "Luna", Sound = "Purr" };
        var dest = mapper.Map<Cat, AnimalDest>(cat);
        dest.Name.Should().Be("Luna");
        dest.Sound.Should().Be("Purr");
    }

    [Fact]
    public void Map_using_runtime_type_resolves_correctly()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Cat, AnimalDest>();
        }).CreateMapper();

        IAnimal animal = new Cat { Name = "Shadow", Sound = "Hiss" };
        // Map<TDest>(object) uses runtime type
        var dest = mapper.Map<AnimalDest>(animal);
        dest.Name.Should().Be("Shadow");
        dest.Sound.Should().Be("Hiss");
    }

    [Fact]
    public void Map_using_untyped_overload_with_interface_source()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Cat, AnimalDest>();
        }).CreateMapper();

        var cat = new Cat { Name = "Felix", Sound = "Meow" };
        var dest = (AnimalDest)mapper.Map(cat, typeof(Cat), typeof(AnimalDest));
        dest.Name.Should().Be("Felix");
    }
}

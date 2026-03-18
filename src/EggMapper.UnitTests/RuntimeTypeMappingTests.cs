using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class RuntimeTypeMappingTests
{
    [Fact]
    public void Map_WithRuntimeTypes_ReturnsCorrectType()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>());
        var mapper = config.CreateMapper();

        object src = new FlatSource { Name = "Runtime", Age = 42 };
        var result = mapper.Map(src, typeof(FlatSource), typeof(FlatDest));

        result.Should().BeOfType<FlatDest>();
        ((FlatDest)result).Name.Should().Be("Runtime");
        ((FlatDest)result).Age.Should().Be(42);
    }

    [Fact]
    public void Map_WithRuntimeTypes_NestedObjects_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        });
        var mapper = config.CreateMapper();

        object src = new PersonSource
        {
            Name = "Jane",
            Address = new AddressSource { Street = "123 Main", City = "NYC" }
        };

        var result = (PersonDest)mapper.Map(src, typeof(PersonSource), typeof(PersonDest));
        result.Name.Should().Be("Jane");
        result.Address!.Street.Should().Be("123 Main");
    }

    [Fact]
    public void Map_UntypedGeneric_ReturnsCorrectResult()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>());
        var mapper = config.CreateMapper();

        object src = new FlatSource { Name = "Generic" };
        var result = mapper.Map<FlatDest>(src);
        result.Name.Should().Be("Generic");
    }
}

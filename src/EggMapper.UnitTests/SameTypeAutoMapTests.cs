using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Same-type auto-mapping: T → T works without explicit CreateMap&lt;T, T&gt;().
/// Creates a property-copy (new instance with all properties copied).
/// </summary>
public class SameTypeAutoMapTests
{
    private class DeliveryProvider
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsActive { get; set; }
    }

    private class Address
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
    }

    private class Person
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public Address? HomeAddress { get; set; }
    }

    [Fact]
    public void Map_SameType_CopiesAllProperties()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var src = new DeliveryProvider { Id = 42, Name = "FedEx", IsActive = true };
        var dest = mapper.Map<DeliveryProvider, DeliveryProvider>(src);

        dest.Should().NotBeSameAs(src);
        dest.Id.Should().Be(42);
        dest.Name.Should().Be("FedEx");
        dest.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Map_SameType_SingleTypeArg_Works()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var src = new DeliveryProvider { Id = 7, Name = "DHL" };
        var dest = mapper.Map<DeliveryProvider>((object)src);

        dest.Should().NotBeSameAs(src);
        dest.Id.Should().Be(7);
        dest.Name.Should().Be("DHL");
    }

    [Fact]
    public void Map_SameType_NullSource_ReturnsDefault()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var dest = mapper.Map<DeliveryProvider, DeliveryProvider>(null!);

        dest.Should().BeNull();
    }

    [Fact]
    public void Map_SameType_WithNestedObject_ShallowCopiesRef()
    {
        // Without CreateMap<Address, Address>, nested Address is reference-copied
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var src = new Person
        {
            Id = 1,
            Name = "Alice",
            HomeAddress = new Address { Street = "123 Main", City = "NYC" }
        };
        var dest = mapper.Map<Person, Person>(src);

        dest.Should().NotBeSameAs(src);
        dest.Id.Should().Be(1);
        dest.Name.Should().Be("Alice");
        dest.HomeAddress.Should().NotBeNull();
    }

    [Fact]
    public void Map_SameType_List_AutoMapsElements()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var list = new List<DeliveryProvider>
        {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        };

        var dest = mapper.Map<List<DeliveryProvider>>((object)list);

        dest.Should().HaveCount(2);
        dest[0].Should().NotBeSameAs(list[0]);
        dest[0].Id.Should().Be(1);
        dest[1].Name.Should().Be("B");
    }

    [Fact]
    public void Map_SameType_SecondCall_UsesFastCache()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var src1 = new DeliveryProvider { Id = 1, Name = "First" };
        var src2 = new DeliveryProvider { Id = 2, Name = "Second" };

        var d1 = mapper.Map<DeliveryProvider, DeliveryProvider>(src1);
        var d2 = mapper.Map<DeliveryProvider, DeliveryProvider>(src2);

        d1.Id.Should().Be(1);
        d2.Id.Should().Be(2);
    }

    [Fact]
    public void Map_SameType_WithExplicitMap_UsesExplicitMap()
    {
        // Explicit CreateMap takes precedence over auto-mapping
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DeliveryProvider, DeliveryProvider>()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name + " (copy)"));
        }).CreateMapper();

        var src = new DeliveryProvider { Id = 1, Name = "UPS" };
        var dest = mapper.Map<DeliveryProvider, DeliveryProvider>(src);

        dest.Name.Should().Be("UPS (copy)");
    }
}

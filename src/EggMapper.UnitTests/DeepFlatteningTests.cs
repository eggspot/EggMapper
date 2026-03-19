using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class DeepFlatteningTests
{
    [Fact]
    public void Map_TwoLevelFlattening_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<Order, OrderDto>());
        var mapper = config.CreateMapper();

        var src = new Order
        {
            Id = 1,
            Customer = new OrderCustomer
            {
                Name = "Alice",
                Address = new OrderAddress { City = "NYC", Zip = "10001" }
            }
        };

        var result = mapper.Map<Order, OrderDto>(src);
        result.Id.Should().Be(1);
        result.CustomerName.Should().Be("Alice");
    }

    [Fact]
    public void Map_FlatteningWithNullIntermediate_LeavesDefault()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<Order, OrderDto>());
        var mapper = config.CreateMapper();

        var src = new Order { Id = 2, Customer = null };
        var result = mapper.Map<Order, OrderDto>(src);
        result.CustomerName.Should().BeNull();
    }

    [Fact]
    public void Map_MultipleNestedFlattening_MapsAllProperties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<Company, CompanyDto>());
        var mapper = config.CreateMapper();

        var src = new Company
        {
            Name = "Acme",
            Ceo = new Person { FirstName = "John" },
            Location = new Location { Country = "US" }
        };

        var result = mapper.Map<Company, CompanyDto>(src);
        result.Name.Should().Be("Acme");
        result.CeoFirstName.Should().Be("John");
        result.LocationCountry.Should().Be("US");
    }
}

file class Order
{
    public int Id { get; set; }
    public OrderCustomer? Customer { get; set; }
}
file class OrderCustomer
{
    public string Name { get; set; } = "";
    public OrderAddress? Address { get; set; }
}
file class OrderAddress
{
    public string City { get; set; } = "";
    public string Zip { get; set; } = "";
}
file class OrderDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
}
file class Company
{
    public string Name { get; set; } = "";
    public Person? Ceo { get; set; }
    public Location? Location { get; set; }
}
file class Person { public string FirstName { get; set; } = ""; }
file class Location { public string Country { get; set; } = ""; }
file class CompanyDto
{
    public string Name { get; set; } = "";
    public string? CeoFirstName { get; set; }
    public string? LocationCountry { get; set; }
}

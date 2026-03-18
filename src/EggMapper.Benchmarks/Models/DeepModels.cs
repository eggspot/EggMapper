namespace EggMapper.Benchmarks.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Address HomeAddress { get; set; } = new();
    public Address WorkAddress { get; set; } = new();
}

public class Address
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Zip { get; set; } = "";
    public string Country { get; set; } = "";
}

public class CustomerDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public AddressDTO HomeAddress { get; set; } = new();
    public AddressDTO WorkAddress { get; set; } = new();
}

public class AddressDTO
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Zip { get; set; } = "";
    public string Country { get; set; } = "";
}

namespace EggMapper.Benchmarks.Models;

public class FlatteningAddress
{
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string Zip { get; set; } = "";
}

public class FlatteningContact
{
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
}

public class FlatteningSource
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public FlatteningAddress Address { get; set; } = new();
    public FlatteningContact Contact { get; set; } = new();
}

public class FlatteningDest
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string AddressStreet { get; set; } = "";
    public string AddressCity { get; set; } = "";
    public string AddressState { get; set; } = "";
    public string AddressZip { get; set; } = "";
    public string ContactEmail { get; set; } = "";
    public string ContactPhone { get; set; } = "";
}

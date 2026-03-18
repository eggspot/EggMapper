namespace EggMapper.Benchmarks.Models;

public class ModelObject
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int BaseDate { get; set; }
    public string? Description { get; set; }
    public double Value { get; set; }
    public bool Active { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string? Country { get; set; }
}

public class ModelDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int BaseDate { get; set; }
    public string? Description { get; set; }
    public double Value { get; set; }
    public bool Active { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public int Status { get; set; }
    public string? Country { get; set; }
}

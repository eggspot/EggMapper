namespace EggMapper.Benchmarks.Models;

public class Foo
{
    public int FooId { get; set; }
    public string FooName { get; set; } = "";
    public InnerFoo Inner { get; set; } = new();
    public List<InnerFoo> InnerFoos { get; set; } = new();
}

public class InnerFoo
{
    public int InnerFooId { get; set; }
    public string InnerFooName { get; set; } = "";
}

public class FooDest
{
    public int FooId { get; set; }
    public string FooName { get; set; } = "";
    public InnerFooDest Inner { get; set; } = new();
    public List<InnerFooDest> InnerFoos { get; set; } = new();
}

public class InnerFooDest
{
    public int InnerFooId { get; set; }
    public string InnerFooName { get; set; } = "";
}

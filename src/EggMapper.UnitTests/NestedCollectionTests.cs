using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class NestedCollectionTests
{
    [Fact]
    public void Maps_Object_With_Nested_List_Of_Complex_Objects()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSource, OrderDest>();
            cfg.CreateMap<OrderLineSource, OrderLineDest>();
        });
        var mapper = config.CreateMapper();

        var src = new OrderSource
        {
            OrderId = 1,
            Customer = "Alice",
            Lines = new List<OrderLineSource>
            {
                new() { ProductName = "Widget", Quantity = 5, Price = 10.0 },
                new() { ProductName = "Gadget", Quantity = 2, Price = 25.0 }
            }
        };

        var dest = mapper.Map<OrderSource, OrderDest>(src);

        dest.OrderId.Should().Be(1);
        dest.Customer.Should().Be("Alice");
        dest.Lines.Should().HaveCount(2);
        dest.Lines[0].ProductName.Should().Be("Widget");
        dest.Lines[0].Quantity.Should().Be(5);
        dest.Lines[1].Price.Should().Be(25.0);
    }

    [Fact]
    public void Maps_Object_With_Array_Of_Complex_Objects()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderArraySource, OrderArrayDest>();
            cfg.CreateMap<OrderLineSource, OrderLineDest>();
        });
        var mapper = config.CreateMapper();

        var src = new OrderArraySource
        {
            OrderId = 2,
            Lines = new[]
            {
                new OrderLineSource { ProductName = "A", Quantity = 1, Price = 5.0 },
                new OrderLineSource { ProductName = "B", Quantity = 3, Price = 15.0 }
            }
        };

        var dest = mapper.Map<OrderArraySource, OrderArrayDest>(src);
        dest.Lines.Should().HaveCount(2);
        dest.Lines[0].ProductName.Should().Be("A");
        dest.Lines[1].Quantity.Should().Be(3);
    }

    [Fact]
    public void MapList_With_NestedCollections_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSource, OrderDest>();
            cfg.CreateMap<OrderLineSource, OrderLineDest>();
        });
        var mapper = config.CreateMapper();

        var sources = new List<OrderSource>
        {
            new() { OrderId = 1, Customer = "A", Lines = new List<OrderLineSource>
                { new() { ProductName = "X", Quantity = 1, Price = 10 } } },
            new() { OrderId = 2, Customer = "B", Lines = new List<OrderLineSource>
                { new() { ProductName = "Y", Quantity = 2, Price = 20 } } }
        };

        var results = mapper.MapList<OrderSource, OrderDest>(sources);

        results.Should().HaveCount(2);
        results[0].Lines.Should().HaveCount(1);
        results[0].Lines[0].ProductName.Should().Be("X");
        results[1].Lines[0].Price.Should().Be(20);
    }

    [Fact]
    public void Maps_Null_NestedCollection_StaysNull()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSource, OrderDest>();
            cfg.CreateMap<OrderLineSource, OrderLineDest>();
        });
        var mapper = config.CreateMapper();

        var src = new OrderSource { OrderId = 1, Customer = "Alice", Lines = null! };
        var dest = mapper.Map<OrderSource, OrderDest>(src);
        dest.Lines.Should().BeNull();
    }

    [Fact]
    public void Maps_Empty_NestedCollection()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSource, OrderDest>();
            cfg.CreateMap<OrderLineSource, OrderLineDest>();
        });
        var mapper = config.CreateMapper();

        var src = new OrderSource { OrderId = 1, Lines = new List<OrderLineSource>() };
        var dest = mapper.Map<OrderSource, OrderDest>(src);
        dest.Lines.Should().NotBeNull();
        dest.Lines.Should().BeEmpty();
    }
}

// ── Test models ─────────────────────────────────────────────────────────────
public class OrderLineSource
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public double Price { get; set; }
}

public class OrderLineDest
{
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public double Price { get; set; }
}

public class OrderSource
{
    public int OrderId { get; set; }
    public string Customer { get; set; } = "";
    public List<OrderLineSource> Lines { get; set; } = new();
}

public class OrderDest
{
    public int OrderId { get; set; }
    public string Customer { get; set; } = "";
    public List<OrderLineDest> Lines { get; set; } = null!;
}

public class OrderArraySource
{
    public int OrderId { get; set; }
    public OrderLineSource[] Lines { get; set; } = Array.Empty<OrderLineSource>();
}

public class OrderArrayDest
{
    public int OrderId { get; set; }
    public OrderLineDest[] Lines { get; set; } = Array.Empty<OrderLineDest>();
}

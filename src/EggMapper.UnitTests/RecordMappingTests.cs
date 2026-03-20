using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Feature 5: Record / Constructor Parameter Mapping.
/// Verifies that EggMapper auto-detects the best constructor when no parameterless ctor exists.
/// </summary>
public class RecordMappingTests
{
    // ── Simple positional record ──────────────────────────────────────────────

    private record PersonSrc(string Name, int Age, string Email);
    private record PersonDest(string Name, int Age, string Email);

    [Fact]
    public void Map_SimpleRecord_MatchesAllCtorParams()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonSrc, PersonDest>());
        var mapper = cfg.CreateMapper();

        var src = new PersonSrc("Alice", 30, "alice@example.com");
        var dest = mapper.Map<PersonSrc, PersonDest>(src);

        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(30);
        dest.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void MapList_SimpleRecord_MatchesAllCtorParams()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonSrc, PersonDest>());
        var mapper = cfg.CreateMapper();

        var src = new List<PersonSrc>
        {
            new("Alice", 30, "alice@example.com"),
            new("Bob",   25, "bob@example.com"),
        };
        var dest = mapper.MapList<PersonSrc, PersonDest>(src);

        dest.Should().HaveCount(2);
        dest[0].Name.Should().Be("Alice");
        dest[1].Name.Should().Be("Bob");
    }

    // ── Record with partial parameter match (subset of props in ctor) ─────────

    private class SourceFull
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Extra { get; set; } = "";
    }

    private record DestPartial(string Name, int Age);

    [Fact]
    public void Map_RecordWithFewerCtorParamsThanSourceProps_MapsMatchedParams()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<SourceFull, DestPartial>());
        var mapper = cfg.CreateMapper();

        var src = new SourceFull { Name = "Charlie", Age = 42, Extra = "ignored" };
        var dest = mapper.Map<SourceFull, DestPartial>(src);

        dest.Name.Should().Be("Charlie");
        dest.Age.Should().Be(42);
    }

    // ── Class with only a parameterized constructor (no init-only) ───────────

    private class SourceSimple
    {
        public string Title { get; set; } = "";
        public double Price { get; set; }
    }

    private class DestNoDefaultCtor
    {
        public string Title { get; }
        public double Price { get; }

        public DestNoDefaultCtor(string title, double price)
        {
            Title = title;
            Price = price;
        }
    }

    [Fact]
    public void Map_ClassWithNoDefaultCtor_UsesParameterizedCtor()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<SourceSimple, DestNoDefaultCtor>());
        var mapper = cfg.CreateMapper();

        var src = new SourceSimple { Title = "Widget", Price = 9.99 };
        var dest = mapper.Map<SourceSimple, DestNoDefaultCtor>(src);

        dest.Title.Should().Be("Widget");
        dest.Price.Should().BeApproximately(9.99, 0.001);
    }

    // ── Record nested inside another mapped type ──────────────────────────────

    private class OrderSrc
    {
        public string OrderId { get; set; } = "";
        public PersonSrc? Customer { get; set; }
    }

    private class OrderDest
    {
        public string OrderId { get; set; } = "";
        public PersonDest? Customer { get; set; }
    }

    [Fact]
    public void Map_NestedRecord_MapsChildRecordViaCtorParams()
    {
        var cfg = new MapperConfiguration(c =>
        {
            c.CreateMap<PersonSrc, PersonDest>();
            c.CreateMap<OrderSrc, OrderDest>();
        });
        var mapper = cfg.CreateMapper();

        var src = new OrderSrc
        {
            OrderId = "ORD-001",
            Customer = new PersonSrc("Dana", 28, "dana@example.com")
        };
        var dest = mapper.Map<OrderSrc, OrderDest>(src);

        dest.OrderId.Should().Be("ORD-001");
        dest.Customer.Should().NotBeNull();
        dest.Customer!.Name.Should().Be("Dana");
        dest.Customer.Age.Should().Be(28);
    }

    // ── Collection of records ─────────────────────────────────────────────────

    private class BatchSrc
    {
        public List<PersonSrc> Members { get; set; } = new();
    }

    private class BatchDest
    {
        public List<PersonDest> Members { get; set; } = new();
    }

    [Fact]
    public void Map_CollectionOfRecords_MapsEachElementViaCtorParams()
    {
        var cfg = new MapperConfiguration(c =>
        {
            c.CreateMap<PersonSrc, PersonDest>();
            c.CreateMap<BatchSrc, BatchDest>();
        });
        var mapper = cfg.CreateMapper();

        var src = new BatchSrc
        {
            Members = new List<PersonSrc>
            {
                new("Eve", 22, "eve@example.com"),
                new("Frank", 35, "frank@example.com"),
            }
        };
        var dest = mapper.Map<BatchSrc, BatchDest>(src);

        dest.Members.Should().HaveCount(2);
        dest.Members[0].Name.Should().Be("Eve");
        dest.Members[1].Name.Should().Be("Frank");
    }

    // ── AssertConfigurationIsValid works for record maps ─────────────────────

    [Fact]
    public void AssertConfigurationIsValid_RecordMap_DoesNotThrow()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonSrc, PersonDest>());
        var act = () => cfg.AssertConfigurationIsValid();
        act.Should().NotThrow();
    }
}

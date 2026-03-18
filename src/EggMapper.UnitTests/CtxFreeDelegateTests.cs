using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

// ── Models used only in this test file ─────────────────────────────────────────
file class ScalarSource
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public bool Active { get; set; }
}

file class ScalarDest
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Value { get; set; }
    public bool Active { get; set; }
}

file class NullableSource
{
    public int? NullableInt { get; set; }
    public double? NullableDouble { get; set; }
}

file class NullableDest
{
    public int NullableInt { get; set; }
    public double NullableDouble { get; set; }
}

file class ValueConvSource { public int IntVal { get; set; } }
file class ValueConvDest   { public long IntVal { get; set; } }

/// <summary>
/// Behavioural tests for the ctx-free <c>Func&lt;TSource, TDestination&gt;</c> fast path
/// used in <see cref="Mapper.MapList{TSource,TDestination}"/> for flat maps.
/// These tests exercise the same scenarios whether the ctx-free or ctx-aware path is used,
/// ensuring both paths produce correct results.
/// </summary>
public class CtxFreeDelegateTests
{
    // ── MapList — flat scalar maps (ctx-free path) ─────────────────────────────

    [Fact]
    public void MapList_FlatScalarMap_IList_MapsAllItems()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>());
        var mapper = config.CreateMapper();

        var sources = new List<ScalarSource>
        {
            new() { Id = 1, Name = "Alice", Value = 1.5, Active = true },
            new() { Id = 2, Name = "Bob",   Value = 2.5, Active = false },
            new() { Id = 3, Name = "Carol", Value = 3.5, Active = true },
        };

        // Act
        var results = mapper.MapList<ScalarSource, ScalarDest>(sources);

        // Assert
        results.Should().HaveCount(3);
        for (int i = 0; i < sources.Count; i++)
        {
            results[i].Id.Should().Be(sources[i].Id);
            results[i].Name.Should().Be(sources[i].Name);
            results[i].Value.Should().Be(sources[i].Value);
            results[i].Active.Should().Be(sources[i].Active);
        }
    }

    [Fact]
    public void MapList_FlatScalarMap_IEnumerable_MapsAllItems()
    {
        // Arrange — non-IList IEnumerable to exercise the foreach branch
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>());
        var mapper = config.CreateMapper();

        IEnumerable<ScalarSource> sources = new[]
        {
            new ScalarSource { Id = 10, Name = "X", Value = 0.1, Active = true },
            new ScalarSource { Id = 20, Name = "Y", Value = 0.2, Active = false },
        }.Where(_ => true); // forces non-IList enumerator

        // Act
        var results = mapper.MapList<ScalarSource, ScalarDest>(sources);

        // Assert
        results.Should().HaveCount(2);
        results[0].Id.Should().Be(10);
        results[0].Name.Should().Be("X");
        results[1].Id.Should().Be(20);
        results[1].Name.Should().Be("Y");
    }

    [Fact]
    public void MapList_NullableToNonNullable_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableSource, NullableDest>());
        var mapper = config.CreateMapper();

        var sources = new List<NullableSource>
        {
            new() { NullableInt = 7,    NullableDouble = 1.5 },
            new() { NullableInt = null, NullableDouble = null },
        };

        var results = mapper.MapList<NullableSource, NullableDest>(sources);

        results.Should().HaveCount(2);
        results[0].NullableInt.Should().Be(7);
        results[0].NullableDouble.Should().Be(1.5);
        results[1].NullableInt.Should().Be(0);     // default when source is null
        results[1].NullableDouble.Should().Be(0.0);
    }

    [Fact]
    public void MapList_ValueTypeNumericConversion_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ValueConvSource, ValueConvDest>());
        var mapper = config.CreateMapper();

        var sources = new List<ValueConvSource>
        {
            new() { IntVal = 1 },
            new() { IntVal = 2147483647 }, // int.MaxValue fits in long
        };

        var results = mapper.MapList<ValueConvSource, ValueConvDest>(sources);

        results.Should().HaveCount(2);
        results[0].IntVal.Should().Be(1L);
        results[1].IntVal.Should().Be(2147483647L);
    }

    // ── MapList — maps requiring ctx (fallback path) ───────────────────────────

    [Fact]
    public void MapList_NestedRegisteredMap_MapsCorrectly()
    {
        // Nested reference-type maps use the ctx-aware fallback path in MapList
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        });
        var mapper = config.CreateMapper();

        var sources = new List<PersonSource>
        {
            new() { Name = "Alice", Age = 30, Address = new AddressSource { Street = "Main St", City = "NYC", Zip = "10001" } },
            new() { Name = "Bob",   Age = 25, Address = null },
        };

        var results = mapper.MapList<PersonSource, PersonDest>(sources);

        results.Should().HaveCount(2);
        results[0].Name.Should().Be("Alice");
        results[0].Address!.City.Should().Be("NYC");
        results[1].Name.Should().Be("Bob");
        results[1].Address.Should().BeNull();
    }

    [Fact]
    public void MapList_MapWithBeforeAfterHooks_HooksFireForEveryItem()
    {
        // Maps with hooks use the ctx-aware fallback path
        int beforeCount = 0, afterCount = 0;

        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>()
               .BeforeMap((s, d) => beforeCount++)
               .AfterMap( (s, d) => afterCount++));
        var mapper = config.CreateMapper();

        var sources = new List<ScalarSource>
        {
            new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 },
        };

        var results = mapper.MapList<ScalarSource, ScalarDest>(sources);

        results.Should().HaveCount(3);
        beforeCount.Should().Be(3);
        afterCount.Should().Be(3);
    }

    [Fact]
    public void MapList_MapWithCondition_ConditionAppliesForEveryItem()
    {
        // Maps with property conditions use the ctx-aware fallback path
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>()
               .ForMember(d => d.Name, o =>
               {
                   o.MapFrom(s => s.Name);
                   o.Condition(s => s.Id > 1);
               }));
        var mapper = config.CreateMapper();

        var sources = new List<ScalarSource>
        {
            new() { Id = 1, Name = "Skip" },
            new() { Id = 2, Name = "Keep" },
        };

        var results = mapper.MapList<ScalarSource, ScalarDest>(sources);

        results.Should().HaveCount(2);
        results[0].Name.Should().Be(""); // condition false → Name left as default
        results[1].Name.Should().Be("Keep");
    }

    // ── MapList — edge cases ───────────────────────────────────────────────────

    [Fact]
    public void MapList_NullSource_ThrowsArgumentNullException()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>());
        var mapper = config.CreateMapper();

        var act = () => mapper.MapList<ScalarSource, ScalarDest>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapList_EmptyList_ReturnsEmptyList()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>());
        var mapper = config.CreateMapper();

        var results = mapper.MapList<ScalarSource, ScalarDest>(new List<ScalarSource>());
        results.Should().BeEmpty();
    }

    [Fact]
    public void MapList_PreSizesResultList_FromIListSource()
    {
        // Result list capacity should equal source count (no resizing)
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<ScalarSource, ScalarDest>());
        var mapper = config.CreateMapper();

        var sources = Enumerable.Range(1, 50)
            .Select(i => new ScalarSource { Id = i, Name = $"Item{i}" })
            .ToList();

        var results = mapper.MapList<ScalarSource, ScalarDest>(sources);

        results.Should().HaveCount(50);
        results.Select(r => r.Id).Should().BeEquivalentTo(sources.Select(s => s.Id));
    }
}


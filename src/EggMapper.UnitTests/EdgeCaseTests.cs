using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class EdgeCaseTests
{
    // ── Empty / whitespace strings ──────────────────────────────────────────
    [Fact]
    public void Map_EmptyString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "", Email = "" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("");
        dest.Email.Should().Be("");
    }

    [Fact]
    public void Map_WhitespaceString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "   ", Email = "\t\n" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("   ");
        dest.Email.Should().Be("\t\n");
    }

    // ── Boundary numeric values ─────────────────────────────────────────────
    [Fact]
    public void Map_IntMaxValue_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Age = int.MaxValue };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Map_IntMinValue_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Age = int.MinValue };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(int.MinValue);
    }

    [Fact]
    public void Map_DoubleSpecialValues_MapCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.MaxValue }).Value.Should().Be(double.MaxValue);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.MinValue }).Value.Should().Be(double.MinValue);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.NaN }).Value.Should().Be(double.NaN);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.PositiveInfinity }).Value.Should().Be(double.PositiveInfinity);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.NegativeInfinity }).Value.Should().Be(double.NegativeInfinity);
    }

    // ── Special characters in strings ───────────────────────────────────────
    [Fact]
    public void Map_UnicodeString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "日本語テスト 🎉 Ñoño" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("日本語テスト 🎉 Ñoño");
    }

    [Fact]
    public void Map_VeryLongString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var longString = new string('x', 100_000);
        var src = new FlatSource { Name = longString };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be(longString);
        dest.Name.Length.Should().Be(100_000);
    }

    // ── DateTime and Guid property mapping ──────────────────────────────────
    [Fact]
    public void Map_DateTimeProperties_MapCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DateTimeSource, DateTimeDest>()).CreateMapper();
        var now = DateTime.UtcNow;
        var src = new DateTimeSource { Created = now, Modified = DateTimeOffset.Now, Id = Guid.NewGuid() };
        var dest = mapper.Map<DateTimeSource, DateTimeDest>(src);
        dest.Created.Should().Be(now);
        dest.Modified.Should().Be(src.Modified);
        dest.Id.Should().Be(src.Id);
    }

    // ── Multiple consecutive mappings ───────────────────────────────────────
    [Fact]
    public void Map_CalledRepeatedly_ProducesIndependentResults()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

        var results = new List<FlatDest>();
        for (int i = 0; i < 100; i++)
        {
            var src = new FlatSource { Name = $"Item{i}", Age = i };
            results.Add(mapper.Map<FlatSource, FlatDest>(src));
        }

        for (int i = 0; i < 100; i++)
        {
            results[i].Name.Should().Be($"Item{i}");
            results[i].Age.Should().Be(i);
        }
    }

    // ── Missing map throws ──────────────────────────────────────────────────
    [Fact]
    public void Map_UnconfiguredMap_ThrowsInvalidOperationException()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var act = () => mapper.Map<PersonSource, PersonDest>(new PersonSource());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("No mapping configured");
    }

    // ── Null source with Map<TDest>(object) ─────────────────────────────────
    [Fact]
    public void Map_NullObjectSource_ThrowsArgumentNullException()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var act = () => mapper.Map<FlatDest>(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

// ── Additional test models ──────────────────────────────────────────────────
public class DateTimeSource
{
    public DateTime Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public Guid Id { get; set; }
}

public class DateTimeDest
{
    public DateTime Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public Guid Id { get; set; }
}

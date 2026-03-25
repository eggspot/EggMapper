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

    // ── Null source with Map<TDest>(object?) returns default ────────────────
    [Fact]
    public void Map_NullObjectSource_ReturnsDefault()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var result = mapper.Map<FlatDest>(null);
        result.Should().BeNull();
    }

    // ── EF Core proxy / derived type resolution ──────────────────────────────
    [Fact]
    public void Map_DerivedSourceType_UsesBaseMapping()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<BaseEntity, BaseDto>()).CreateMapper();
        object source = new DerivedProxy { Id = 42, Name = "test" };
        var result = mapper.Map<BaseDto>(source);
        result.Id.Should().Be(42);
        result.Name.Should().Be("test");
    }

    [Fact]
    public void Map_TypedWithDerivedRuntimeType_UsesBaseMapping()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<BaseEntity, BaseDto>()).CreateMapper();
        BaseEntity source = new DerivedProxy { Id = 7, Name = "proxy" };
        var result = mapper.Map<BaseEntity, BaseDto>(source);
        result.Id.Should().Be(7);
        result.Name.Should().Be("proxy");
    }

    [Fact]
    public void Map_CollectionOfDerivedElements_UsesBaseMapping()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<BaseEntity, BaseDto>()).CreateMapper();
        var source = new List<BaseEntity>
        {
            new DerivedProxy { Id = 1, Name = "a" },
            new DerivedProxy { Id = 2, Name = "b" }
        };
        var result = mapper.MapList<BaseEntity, BaseDto>(source);
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Name.Should().Be("b");
    }

    // ── MapFrom(s => s) with constructor-based conversion (DSP Id<T> pattern) ─
    [Fact]
    public void Map_MapFromSourceEntity_ConvertsViaConstructor()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EntityWithId, StrongIdDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s)))
            .CreateMapper();

        var src = new EntityWithId { Id = 42, Name = "test" };
        var result = mapper.Map<EntityWithId, StrongIdDto>(src);

        result.Id.Should().NotBeNull();
        result.Id!.Value.Should().Be(42);
        result.Name.Should().Be("test");
    }

    [Fact]
    public void Map_MapFromSourceEntity_WorksInCollectionWithAfterMap()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EntityWithId, StrongIdDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s)))
            .CreateMapper();

        var sources = new List<EntityWithId>
        {
            new() { Id = 1, Name = "a" },
            new() { Id = 2, Name = "b" }
        };

        var result = mapper.Map<List<StrongIdDto>>(sources, opt =>
        {
            opt.AfterMap((src, dest) =>
            {
                dest.ForEach(d =>
                {
                    var original = sources.FirstOrDefault(s => s.Id == d.Id!.Value);
                    d.Name = original?.Name + "_mapped";
                });
            });
        });

        result.Should().HaveCount(2);
        result[0].Id!.Value.Should().Be(1);
        result[0].Name.Should().Be("a_mapped");
        result[1].Id!.Value.Should().Be(2);
        result[1].Name.Should().Be("b_mapped");
    }
}

// ── Additional test models ──────────────────────────────────────────────────
public class BaseEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class DerivedProxy : BaseEntity
{
    // Simulates EF Core lazy-loading proxy
}

public class BaseDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// ── Strongly-typed ID models (simulates DSP's Id<T> pattern) ─────────────────
public interface IHasId { int Id { get; } }

public class EntityWithId : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class StrongId
{
    public int Value { get; }
    public StrongId() { }
    public StrongId(IHasId entity) { Value = entity.Id; }
}

public class StrongIdDto
{
    public StrongId? Id { get; set; }
    public string? Name { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
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

using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Feature 6: Patch / Partial Mapping.
/// Verifies that Patch() only overwrites destination properties where the source
/// property is non-null (reference types) or HasValue (Nullable&lt;T&gt;).
/// </summary>
public class PatchMappingTests
{
    private class PersonPatch
    {
        public string? Name { get; set; }
        public int? Age { get; set; }
        public string? Email { get; set; }
    }

    private class PersonEntity
    {
        public string Name { get; set; } = "Original";
        public int Age { get; set; } = 99;
        public string Email { get; set; } = "original@example.com";
    }

    // ── Basic null-skipping ────────────────────────────────────────────────

    [Fact]
    public void Patch_NullSourceProps_LeavesDestinationUnchanged()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonPatch, PersonEntity>());
        var mapper = cfg.CreateMapper();

        var patch = new PersonPatch { Name = null, Age = null, Email = null };
        var dest = new PersonEntity { Name = "Alice", Age = 30, Email = "alice@example.com" };

        mapper.Patch(patch, dest);

        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(30);
        dest.Email.Should().Be("alice@example.com");
    }

    [Fact]
    public void Patch_SomeNullSourceProps_OnlyOverwritesNonNull()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonPatch, PersonEntity>());
        var mapper = cfg.CreateMapper();

        var patch = new PersonPatch { Name = "Bob", Age = null, Email = null };
        var dest = new PersonEntity { Name = "Alice", Age = 30, Email = "alice@example.com" };

        mapper.Patch(patch, dest);

        dest.Name.Should().Be("Bob");
        dest.Age.Should().Be(30);       // not overwritten
        dest.Email.Should().Be("alice@example.com"); // not overwritten
    }

    [Fact]
    public void Patch_AllSourcePropsSet_OverwritesAll()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonPatch, PersonEntity>());
        var mapper = cfg.CreateMapper();

        var patch = new PersonPatch { Name = "Charlie", Age = 25, Email = "charlie@example.com" };
        var dest = new PersonEntity { Name = "Alice", Age = 30, Email = "alice@example.com" };

        mapper.Patch(patch, dest);

        dest.Name.Should().Be("Charlie");
        dest.Age.Should().Be(25);
        dest.Email.Should().Be("charlie@example.com");
    }

    // ── Nullable value types ──────────────────────────────────────────────

    private class UpdateDto
    {
        public int? Count { get; set; }
        public decimal? Price { get; set; }
        public bool? IsActive { get; set; }
    }

    private class Product
    {
        public int Count { get; set; } = 10;
        public decimal Price { get; set; } = 9.99m;
        public bool IsActive { get; set; } = true;
    }

    [Fact]
    public void Patch_NullableValueTypes_OnlyOverwritesHasValue()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<UpdateDto, Product>());
        var mapper = cfg.CreateMapper();

        var dto = new UpdateDto { Count = 5, Price = null, IsActive = false };
        var product = new Product { Count = 10, Price = 9.99m, IsActive = true };

        mapper.Patch(dto, product);

        product.Count.Should().Be(5);
        product.Price.Should().Be(9.99m);    // not overwritten
        product.IsActive.Should().BeFalse(); // overwritten with false
    }

    // ── Non-nullable value types always assigned ──────────────────────────

    private class DirectUpdate
    {
        public int Score { get; set; }
        public string? Label { get; set; }
    }

    private class ScoreEntity
    {
        public int Score { get; set; } = 100;
        public string Label { get; set; } = "default";
    }

    [Fact]
    public void Patch_NonNullableValueType_AlwaysAssigned()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<DirectUpdate, ScoreEntity>());
        var mapper = cfg.CreateMapper();

        // Score = 0 (non-nullable int default) → still assigned
        var dto = new DirectUpdate { Score = 0, Label = null };
        var entity = new ScoreEntity { Score = 100, Label = "original" };

        mapper.Patch(dto, entity);

        entity.Score.Should().Be(0);        // assigned even when 0
        entity.Label.Should().Be("original"); // not overwritten (null string)
    }

    // ── Null source → destination unchanged ──────────────────────────────

    [Fact]
    public void Patch_NullSource_ReturnDestinationUnchanged()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonPatch, PersonEntity>());
        var mapper = cfg.CreateMapper();

        var dest = new PersonEntity { Name = "Eve", Age = 28, Email = "eve@example.com" };

        mapper.Patch<PersonPatch, PersonEntity>(null!, dest);

        dest.Name.Should().Be("Eve");
        dest.Age.Should().Be(28);
        dest.Email.Should().Be("eve@example.com");
    }

    // ── Idempotence: calling Patch twice is safe ─────────────────────────

    [Fact]
    public void Patch_CalledTwice_SecondCallUsesCache()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<PersonPatch, PersonEntity>());
        var mapper = cfg.CreateMapper();

        var dest = new PersonEntity();

        mapper.Patch(new PersonPatch { Name = "First" }, dest);
        dest.Name.Should().Be("First");

        mapper.Patch(new PersonPatch { Name = "Second" }, dest);
        dest.Name.Should().Be("Second");
    }
}

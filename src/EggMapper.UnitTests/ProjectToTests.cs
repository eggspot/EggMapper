using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Feature 8: ProjectTo / IQueryable projection.
/// Validates that BuildProjection and ProjectTo produce correct expression trees
/// and return the right data when executed via LINQ-to-objects.
/// </summary>
public class ProjectToTests
{
    // ── Source / destination types ─────────────────────────────────────────

    private class UserSrc
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Email { get; set; } = "";
        public AddressSrc Address { get; set; } = new();
    }

    private class AddressSrc
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
    }

    private class UserDto
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
        public string Email { get; set; } = "";
    }

    private class UserWithAddressDto
    {
        public string Name { get; set; } = "";
        public AddressDto Address { get; set; } = new();
    }

    private class AddressDto
    {
        public string Street { get; set; } = "";
        public string City { get; set; } = "";
    }

    private class UserFlatDto
    {
        public string Name { get; set; } = "";
        public string AddressStreet { get; set; } = "";
        public string AddressCity { get; set; } = "";
    }

    private class UserCustomDto
    {
        public string DisplayName { get; set; } = "";
        public int Age { get; set; }
    }

    private record UserRecord(string Name, int Age);

    // ── Basic flat projection ──────────────────────────────────────────────

    [Fact]
    public void ProjectTo_BasicFlat_MapsCorrectly()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<UserSrc, UserDto>());

        var source = new List<UserSrc>
        {
            new() { Name = "Alice", Age = 30, Email = "alice@example.com" },
            new() { Name = "Bob",   Age = 25, Email = "bob@example.com" }
        }.AsQueryable();

        var result = source.ProjectTo<UserSrc, UserDto>(cfg).ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alice");
        result[0].Age.Should().Be(30);
        result[0].Email.Should().Be("alice@example.com");
        result[1].Name.Should().Be("Bob");
        result[1].Age.Should().Be(25);
    }

    // ── Nested DTO projection ──────────────────────────────────────────────

    [Fact]
    public void ProjectTo_NestedDto_MapsNestedObjectCorrectly()
    {
        var cfg = new MapperConfiguration(c =>
        {
            c.CreateMap<AddressSrc, AddressDto>();
            c.CreateMap<UserSrc, UserWithAddressDto>();
        });

        var source = new List<UserSrc>
        {
            new() { Name = "Alice", Address = new() { Street = "Main St", City = "Springfield" } }
        }.AsQueryable();

        var result = source.ProjectTo<UserSrc, UserWithAddressDto>(cfg).ToList();

        result[0].Name.Should().Be("Alice");
        result[0].Address.Street.Should().Be("Main St");
        result[0].Address.City.Should().Be("Springfield");
    }

    // ── Flattened projection ───────────────────────────────────────────────

    [Fact]
    public void ProjectTo_FlattenedProperties_MapsCorrectly()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<UserSrc, UserFlatDto>());

        var source = new List<UserSrc>
        {
            new() { Name = "Alice", Address = new() { Street = "Elm St", City = "Shelbyville" } }
        }.AsQueryable();

        var result = source.ProjectTo<UserSrc, UserFlatDto>(cfg).ToList();

        result[0].Name.Should().Be("Alice");
        result[0].AddressStreet.Should().Be("Elm St");
        result[0].AddressCity.Should().Be("Shelbyville");
    }

    // ── Custom MapFrom expression ──────────────────────────────────────────

    [Fact]
    public void ProjectTo_CustomMapFrom_UsesStoredExpression()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserCustomDto>()
             .ForMember(d => d.DisplayName, o => o.MapFrom(s => s.Name + " (user)")));

        var source = new List<UserSrc>
        {
            new() { Name = "Alice", Age = 30 }
        }.AsQueryable();

        var result = source.ProjectTo<UserSrc, UserCustomDto>(cfg).ToList();

        result[0].DisplayName.Should().Be("Alice (user)");
        result[0].Age.Should().Be(30);
    }

    // ── Record (parameterized ctor) projection ─────────────────────────────

    [Fact]
    public void ProjectTo_RecordDestination_MapsViaConstructor()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<UserSrc, UserRecord>());

        var source = new List<UserSrc>
        {
            new() { Name = "Alice", Age = 30 },
            new() { Name = "Bob",   Age = 25 }
        }.AsQueryable();

        var result = source.ProjectTo<UserSrc, UserRecord>(cfg).ToList();

        result[0].Name.Should().Be("Alice");
        result[0].Age.Should().Be(30);
        result[1].Name.Should().Be("Bob");
        result[1].Age.Should().Be(25);
    }

    // ── BuildProjection returns a non-null expression ──────────────────────

    [Fact]
    public void BuildProjection_ReturnsNonNullExpression()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<UserSrc, UserDto>());

        var expr = cfg.BuildProjection<UserSrc, UserDto>();

        expr.Should().NotBeNull();
    }

    // ── Multiple items projected correctly ────────────────────────────────

    [Fact]
    public void ProjectTo_MultipleItems_AllMappedCorrectly()
    {
        var cfg = new MapperConfiguration(c => c.CreateMap<UserSrc, UserDto>());

        var source = Enumerable.Range(1, 5)
            .Select(i => new UserSrc { Name = $"User{i}", Age = 20 + i })
            .AsQueryable();

        var result = source.ProjectTo<UserSrc, UserDto>(cfg).ToList();

        result.Should().HaveCount(5);
        for (int i = 0; i < 5; i++)
        {
            result[i].Name.Should().Be($"User{i + 1}");
            result[i].Age.Should().Be(21 + i);
        }
    }
}

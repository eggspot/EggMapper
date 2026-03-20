using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Feature 7: Inline Validation During Mapping.
/// Validates that .Validate() rules are evaluated after mapping and that
/// MappingValidationException is thrown with all collected violations.
/// </summary>
public class InlineValidationTests
{
    private class UserSrc
    {
        public string Email { get; set; } = "";
        public int Age { get; set; }
        public string Name { get; set; } = "";
    }

    private class UserDest
    {
        public string Email { get; set; } = "";
        public int Age { get; set; }
        public string Name { get; set; } = "";
    }

    // ── Single rule passes ─────────────────────────────────────────────────

    [Fact]
    public void Map_ValidSource_DoesNotThrow()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserDest>()
             .Validate(x => x.Email, e => e.Contains("@"), "Email must contain @"));
        var mapper = cfg.CreateMapper();

        var act = () => mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "alice@example.com", Age = 30, Name = "Alice" });

        act.Should().NotThrow();
    }

    // ── Single rule fails ─────────────────────────────────────────────────

    [Fact]
    public void Map_InvalidSource_ThrowsMappingValidationException()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserDest>()
             .Validate(x => x.Email, e => e.Contains("@"), "Email must contain @"));
        var mapper = cfg.CreateMapper();

        var act = () => mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "not-an-email", Age = 30, Name = "Bob" });

        act.Should().Throw<MappingValidationException>()
           .Which.Errors.Should().ContainSingle()
           .Which.Should().Be("Email must contain @");
    }

    // ── Multiple rules — all failures collected ───────────────────────────

    [Fact]
    public void Map_MultipleFailures_AllCollectedInException()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserDest>()
             .Validate(x => x.Email, e => e.Contains("@"), "Email must contain @")
             .Validate(x => x.Age,   a => a >= 18,          "Age must be 18 or older")
             .Validate(x => x.Name,  n => n.Length > 0,     "Name must not be empty"));
        var mapper = cfg.CreateMapper();

        var act = () => mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "bad", Age = 16, Name = "" });

        var ex = act.Should().Throw<MappingValidationException>().Which;
        ex.Errors.Should().HaveCount(3);
        ex.Errors.Should().Contain("Email must contain @");
        ex.Errors.Should().Contain("Age must be 18 or older");
        ex.Errors.Should().Contain("Name must not be empty");
    }

    // ── Partial failures — only failed rules in exception ─────────────────

    [Fact]
    public void Map_SomeRulesFail_OnlyFailedRulesInErrors()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserDest>()
             .Validate(x => x.Email, e => e.Contains("@"), "Invalid email")
             .Validate(x => x.Age,   a => a >= 18,          "Underage")
             .Validate(x => x.Name,  n => n.Length > 0,     "Name required"));
        var mapper = cfg.CreateMapper();

        var act = () => mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "alice@example.com", Age = 16, Name = "Alice" });

        var ex = act.Should().Throw<MappingValidationException>().Which;
        ex.Errors.Should().ContainSingle().Which.Should().Be("Underage");
    }

    // ── Zero-cost when no validators ──────────────────────────────────────

    [Fact]
    public void Map_NoValidators_UsesCtxFreePath_NoThrow()
    {
        // Without Validate(), the map uses the fast ctx-free path — just verify it still works.
        var cfg = new MapperConfiguration(c => c.CreateMap<UserSrc, UserDest>());
        var mapper = cfg.CreateMapper();

        var dest = mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "alice@example.com", Age = 30, Name = "Alice" });

        dest.Email.Should().Be("alice@example.com");
        dest.Age.Should().Be(30);
    }

    // ── Exception message includes all violations ─────────────────────────

    [Fact]
    public void MappingValidationException_MessageContainsAllViolations()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserDest>()
             .Validate(x => x.Email, e => e.Contains("@"), "Bad email")
             .Validate(x => x.Age,   a => a >= 18,          "Underage"));
        var mapper = cfg.CreateMapper();

        var act = () => mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "bad", Age = 16, Name = "X" });

        var ex = act.Should().Throw<MappingValidationException>().Which;
        ex.Message.Should().Contain("Bad email");
        ex.Message.Should().Contain("Underage");
    }

    // ── All rules pass → no exception ────────────────────────────────────

    [Fact]
    public void Map_AllRulesPass_ReturnsCorrectlyMappedDest()
    {
        var cfg = new MapperConfiguration(c =>
            c.CreateMap<UserSrc, UserDest>()
             .Validate(x => x.Email, e => e.Contains("@"), "Invalid email")
             .Validate(x => x.Age,   a => a > 0,            "Invalid age"));
        var mapper = cfg.CreateMapper();

        var dest = mapper.Map<UserSrc, UserDest>(
            new UserSrc { Email = "test@test.com", Age = 25, Name = "Test" });

        dest.Email.Should().Be("test@test.com");
        dest.Age.Should().Be(25);
        dest.Name.Should().Be("Test");
    }
}

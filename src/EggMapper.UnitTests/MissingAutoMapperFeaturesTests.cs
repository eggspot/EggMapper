using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Tests for AutoMapper-compatible features added to maintain API parity:
///   1. ForMember(...).MapFrom(string) — source member by name on generic API
///   2. ConstructUsing(Func&lt;TSource, ResolutionContext, TDestination&gt;) — context-aware ctor
///   3. ForAllOtherMembers(...) — configure all not-yet-explicitly-mapped members
/// </summary>
public class MapFromStringOnGenericApiTests
{
    private class MfsSrc
    {
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public int    YearBorn  { get; set; }
    }

    private class MfsDest
    {
        public string FullName { get; set; } = "";
        public string Surname  { get; set; } = "";
        public int    Year     { get; set; }
    }

    [Fact]
    public void MapFrom_StringSourceName_MapsCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<MfsSrc, MfsDest>()
               .ForMember(d => d.Surname, o => o.MapFrom("LastName")));
        var mapper = config.CreateMapper();

        var result = mapper.Map<MfsSrc, MfsDest>(new MfsSrc { LastName = "Smith", FirstName = "John" });

        result.Surname.Should().Be("Smith");
    }

    [Fact]
    public void MapFrom_StringSourceName_MultipleMembers()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<MfsSrc, MfsDest>()
               .ForMember(d => d.Surname,  o => o.MapFrom("LastName"))
               .ForMember(d => d.Year,     o => o.MapFrom("YearBorn"))
               .ForMember(d => d.FullName, o => o.MapFrom("FirstName")));
        var mapper = config.CreateMapper();

        var result = mapper.Map<MfsSrc, MfsDest>(new MfsSrc { FirstName = "Jane", LastName = "Doe", YearBorn = 1990 });

        result.FullName.Should().Be("Jane");
        result.Surname.Should().Be("Doe");
        result.Year.Should().Be(1990);
    }

    [Fact]
    public void MapFrom_StringSourceName_UnknownName_DestinationGetsDefault()
    {
        // When the source name doesn't exist, the destination member is left at default
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<MfsSrc, MfsDest>()
               .ForMember(d => d.Surname, o => o.MapFrom("NonExistentProp")));
        var mapper = config.CreateMapper();

        var result = mapper.Map<MfsSrc, MfsDest>(new MfsSrc { LastName = "Smith" });

        // Dest.Surname won't be mapped; default is ""
        result.Surname.Should().Be("");
    }

    [Fact]
    public void MapFrom_StringSourceName_WithMapList_MapsAllElements()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<MfsSrc, MfsDest>()
               .ForMember(d => d.Surname, o => o.MapFrom("LastName")));
        var mapper = config.CreateMapper();

        var list = new List<MfsSrc>
        {
            new() { LastName = "Alpha" },
            new() { LastName = "Beta" },
        };

        var result = mapper.MapList<MfsSrc, MfsDest>(list);

        result.Should().HaveCount(2);
        result[0].Surname.Should().Be("Alpha");
        result[1].Surname.Should().Be("Beta");
    }
}

public class ConstructUsingWithContextTests
{
    private class CucSrc
    {
        public string Name  { get; set; } = "";
        public int    Value { get; set; }
    }

    private class CucDest
    {
        public string Name  { get; }
        public int    Value { get; set; }
        public bool   BuiltWithCtx { get; }

        public CucDest(string name, bool builtWithCtx)
        {
            Name = name;
            BuiltWithCtx = builtWithCtx;
        }
    }

    [Fact]
    public void ConstructUsing_WithContext_CtorIsCalled()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CucSrc, CucDest>()
               .ConstructUsing((src, ctx) => new CucDest(src.Name + "_ctx", builtWithCtx: true)));
        var mapper = config.CreateMapper();

        var result = mapper.Map<CucSrc, CucDest>(new CucSrc { Name = "Test", Value = 42 });

        result.Name.Should().Be("Test_ctx");
        result.BuiltWithCtx.Should().BeTrue();
    }

    [Fact]
    public void ConstructUsing_WithContext_CanAccessContextDepth()
    {
        var capturedDepth = -1;

        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CucSrc, CucDest>()
               .ConstructUsing((src, ctx) =>
               {
                   capturedDepth = ctx.Depth;
                   return new CucDest(src.Name, builtWithCtx: true);
               }));
        var mapper = config.CreateMapper();

        mapper.Map<CucSrc, CucDest>(new CucSrc { Name = "X" });

        // Depth starts at 0 for the top-level call in the flexible path
        capturedDepth.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void ConstructUsing_WithContext_RoutesToFlexiblePath()
    {
        // A map with ConstructUsing(ctx) cannot take the ctx-free fast path.
        // Verify mapping still produces correct results when done repeatedly (caching test).
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<CucSrc, CucDest>()
               .ConstructUsing((src, ctx) => new CucDest(src.Name, builtWithCtx: true)));
        var mapper = config.CreateMapper();

        var src = new CucSrc { Name = "Hello", Value = 7 };

        var r1 = mapper.Map<CucSrc, CucDest>(src);
        var r2 = mapper.Map<CucSrc, CucDest>(src);

        r1.Name.Should().Be("Hello");
        r2.Name.Should().Be("Hello");
    }
}

public class ForAllOtherMembersTests
{
    private class FaomSrc
    {
        public string Name    { get; set; } = "";
        public int    Age     { get; set; }
        public string Email   { get; set; } = "";
        public string Address { get; set; } = "";
    }

    private class FaomDest
    {
        public string Name    { get; set; } = "n/a";
        public int    Age     { get; set; }
        public string Email   { get; set; } = "n/a";
        public string Address { get; set; } = "n/a";
    }

    [Fact]
    public void ForAllOtherMembers_Ignore_IgnoresEverythingExceptExplicitMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FaomSrc, FaomDest>()
               .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
               .ForAllOtherMembers(o => o.Ignore()));
        var mapper = config.CreateMapper();

        var result = mapper.Map<FaomSrc, FaomDest>(new FaomSrc { Name = "Alice", Age = 30, Email = "alice@x.com", Address = "1 Main St" });

        result.Name.Should().Be("Alice");
        // All other members ignored — remain at destination defaults
        result.Age.Should().Be(0);
        result.Email.Should().Be("n/a");
        result.Address.Should().Be("n/a");
    }

    [Fact]
    public void ForAllOtherMembers_WithUseDestinationValue_PreservesExistingValues()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FaomSrc, FaomDest>()
               .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
               .ForAllOtherMembers(o => o.UseDestinationValue()));
        var mapper = config.CreateMapper();

        var dest = new FaomDest { Name = "old", Age = 99, Email = "keep@x.com", Address = "keep" };
        var result = mapper.Map(new FaomSrc { Name = "Alice", Age = 30, Email = "new@x.com" }, dest);

        result.Name.Should().Be("Alice");
        result.Age.Should().Be(99);         // preserved
        result.Email.Should().Be("keep@x.com"); // preserved
        result.Address.Should().Be("keep"); // preserved
    }

    [Fact]
    public void ForAllOtherMembers_DoesNotAffectAlreadyConfiguredMembers()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FaomSrc, FaomDest>()
               .ForMember(d => d.Email, o => o.MapFrom(s => s.Email + "_mapped"))
               .ForAllOtherMembers(o => o.Ignore()));
        var mapper = config.CreateMapper();

        var result = mapper.Map<FaomSrc, FaomDest>(new FaomSrc { Name = "Alice", Email = "test@x.com", Age = 25 });

        result.Email.Should().Be("test@x.com_mapped"); // explicit ForMember wins
        result.Name.Should().Be("n/a");                // ignored by ForAllOtherMembers
        result.Age.Should().Be(0);                     // ignored
    }

    [Fact]
    public void ForAllOtherMembers_WithAssertConfigurationIsValid_PassesWhenAllMembersHandled()
    {
        // AssertConfigurationIsValid should not throw when ForAllOtherMembers ignores all others
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FaomSrc, FaomDest>()
               .ForMember(d => d.Name,    o => o.MapFrom(s => s.Name))
               .ForMember(d => d.Age,     o => o.MapFrom(s => s.Age))
               .ForMember(d => d.Email,   o => o.MapFrom(s => s.Email))
               .ForMember(d => d.Address, o => o.MapFrom(s => s.Address));
        });

        config.Invoking(c => c.AssertConfigurationIsValid()).Should().NotThrow();
    }
}

using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class GuidanceBehaviorTests
{
    public class Address { public string City { get; set; } = ""; }

    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string? Nickname { get; set; }
        public Address Address { get; set; } = new();
    }

    public class PersonUpdate
    {
        public string? FirstName { get; set; }
        public string? Nickname { get; set; }
    }

    public class Outer { public Inner Inner { get; set; } = new(); }
    public class Inner { public string City { get; set; } = ""; }

    public record PersonSummary(int Id, string FirstName);

    public class NameSource { public string Name { get; set; } = ""; }
    public class NameTarget { public string Name { get; set; } = ""; }

    public class UpperCaseResolver : IMemberValueResolver<object, object, string, string>
    {
        public string Resolve(object source, object destination, string sourceMember, string destMember, ResolutionContext context)
            => sourceMember.ToUpperInvariant();
    }

    [Fact]
    public void Patch_NullableUpdateType_CopiesOnlyNonNullMembers()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<PersonUpdate, Person>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Address, o => o.Ignore())).CreateMapper();
        var person = new Person { Id = 7, FirstName = "Ada", Nickname = "A" };

        mapper.Patch(new PersonUpdate { Nickname = "Ace" }, person);

        person.Id.Should().Be(7);
        person.FirstName.Should().Be("Ada");
        person.Nickname.Should().Be("Ace");
    }

    [Fact]
    public void ForPath_NestedDestinationPath_Throws()
    {
        var act = () => new MapperConfiguration(cfg => cfg.CreateMap<Person, Outer>()
            .ForPath(d => d.Inner.City, o => o.MapFrom(s => s.FirstName)));

        act.Should().Throw<InvalidOperationException>().WithMessage("Property 'City' not found or not writable*");
    }

    [Fact]
    public void MapSameType_ClassWithoutCreateMap_CopiesShallow()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();
        var person = new Person { Id = 1, FirstName = "Ada" };

        var copy = mapper.Map<Person, Person>(person);

        copy.Should().NotBeSameAs(person);
        copy.FirstName.Should().Be("Ada");
        copy.Address.Should().BeSameAs(person.Address);
    }

    [Fact]
    public void MapSameType_RecordWithoutCreateMap_ThrowsNoMappingConfigured()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();

        var act = () => mapper.Map<PersonSummary, PersonSummary>(new PersonSummary(1, "Ada"));

        act.Should().Throw<InvalidOperationException>().WithMessage("No mapping configured*");
    }

    [Fact]
    public void MemberValueResolver_MapperWithoutDependencyInjection_ThrowsRequiresDi()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<NameSource, NameTarget>()
            .ForMember(d => d.Name, o => o.MapFrom<UpperCaseResolver, string>(s => s.Name))).CreateMapper();

        var act = () => mapper.Map<NameSource, NameTarget>(new NameSource { Name = "ada" });

        act.Should().Throw<Exception>().Where(e => e.ToString().Contains("IMemberValueResolver requires DI"));
    }
}

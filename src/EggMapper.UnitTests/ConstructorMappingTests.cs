using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ConstructorMappingTests
{
    [Fact]
    public void ConstructUsing_creates_instance_from_custom_factory()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PersonSourceSimple, ImmutablePerson>()
               .ConstructUsing(s => new ImmutablePerson(s.Name, s.Age));
        }).CreateMapper();

        var src = new PersonSourceSimple { Name = "Alice", Age = 30 };
        var dest = mapper.Map<PersonSourceSimple, ImmutablePerson>(src);
        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(30);
    }

    [Fact]
    public void ConstructUsing_can_derive_values_from_source()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PersonSourceSimple, ImmutablePerson>()
               .ConstructUsing(s => new ImmutablePerson(s.Name.ToUpper(), s.Age * 2));
        }).CreateMapper();

        var src = new PersonSourceSimple { Name = "bob", Age = 10 };
        var dest = mapper.Map<PersonSourceSimple, ImmutablePerson>(src);
        dest.Name.Should().Be("BOB");
        dest.Age.Should().Be(20);
    }

    [Fact]
    public void Maps_mutable_record_type_by_convention()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<PersonSourceSimple, MutablePersonRecord>()).CreateMapper();

        var src = new PersonSourceSimple { Name = "Carol", Age = 22 };
        var dest = mapper.Map<PersonSourceSimple, MutablePersonRecord>(src);
        dest.Name.Should().Be("Carol");
        dest.Age.Should().Be(22);
    }

    [Fact]
    public void ConstructUsing_plus_settable_extra_property()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SemiImmutableSource, SemiImmutable>()
               .ConstructUsing(s => new SemiImmutable(s.Name, s.Age));
        }).CreateMapper();

        var src = new SemiImmutableSource { Name = "Dan", Age = 40, Notes = "VIP" };
        var dest = mapper.Map<SemiImmutableSource, SemiImmutable>(src);
        dest.Name.Should().Be("Dan");
        dest.Age.Should().Be(40);
        dest.Notes.Should().Be("VIP");
    }

    [Fact]
    public void ConstructUsing_overrides_default_constructor()
    {
        var ctorCalled = false;
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ConstructUsing(s =>
               {
                   ctorCalled = true;
                   return new FlatDest { Name = "from-ctor" };
               });
        }).CreateMapper();

        var src = new FlatSource { Name = "ignored" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        ctorCalled.Should().BeTrue();
        // Properties are still mapped after custom ctor
        dest.Name.Should().Be("ignored");
    }

    [Fact]
    public void ConstructUsing_null_source_returns_default()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<PersonSourceSimple, ImmutablePerson>()
               .ConstructUsing(s => new ImmutablePerson(s.Name, s.Age));
        }).CreateMapper();

        var result = mapper.Map<PersonSourceSimple, ImmutablePerson>(null!);
        result.Should().BeNull();
    }
}

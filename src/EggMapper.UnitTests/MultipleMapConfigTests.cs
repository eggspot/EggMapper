using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class MultipleMapConfigTests
{
    [Fact]
    public void MultipleCreateMaps_AllResolveCorrectly()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>();
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
            cfg.CreateMap<ItemSource, ItemDest>();
        });
        var mapper = config.CreateMapper();

        mapper.Map<FlatSource, FlatDest>(new FlatSource { Name = "A" }).Name.Should().Be("A");
        mapper.Map<AddressSource, AddressDest>(new AddressSource { City = "NYC" }).City.Should().Be("NYC");
        mapper.Map<PersonSource, PersonDest>(new PersonSource { Name = "Bob" }).Name.Should().Be("Bob");
        mapper.Map<ItemSource, ItemDest>(new ItemSource { Id = 42, Label = "X" }).Id.Should().Be(42);
    }

    [Fact]
    public void DuplicateCreateMap_LastConfigWins()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>();
            cfg.CreateMap<FlatSource, FlatDest>()
                .ForMember(d => d.Name, opts => opts.UseValue("Override"));
        });
        var mapper = config.CreateMapper();

        var result = mapper.Map<FlatSource, FlatDest>(new FlatSource { Name = "Original" });
        result.Name.Should().Be("Override");
    }

    [Fact]
    public void ForMember_ChainingMultipleProperties_AllApplied()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
                .ForMember(d => d.Name, opts => opts.MapFrom(s => s.Email))
                .ForMember(d => d.Email, opts => opts.MapFrom(s => s.Name))
                .ForMember(d => d.Age, opts => opts.UseValue(100))
                .ForMember(d => d.IsActive, opts => opts.Ignore());
        });
        var mapper = config.CreateMapper();

        var src = new FlatSource { Name = "Alice", Email = "alice@test.com", Age = 30, IsActive = true };
        var dest = mapper.Map<FlatSource, FlatDest>(src);

        dest.Name.Should().Be("alice@test.com");  // swapped
        dest.Email.Should().Be("Alice");           // swapped
        dest.Age.Should().Be(100);                 // UseValue
        dest.IsActive.Should().BeFalse();          // Ignored
    }

    [Fact]
    public void Profiles_CanBeUsedTogether()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FlatMappingProfile>();
            cfg.AddProfile<PersonMappingProfile>();
        });
        var mapper = config.CreateMapper();

        mapper.Map<FlatSource, FlatDest>(new FlatSource { Name = "Test" }).Name.Should().Be("Test");
        mapper.Map<PersonSource, PersonDest>(new PersonSource { Name = "Bob", Age = 25 }).Age.Should().Be(25);
    }

    [Fact]
    public void ForAllMembers_IgnoreAll_LeavesAllDefault()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
                .ForAllMembers(opts => opts.Ignore());
        });
        var mapper = config.CreateMapper();

        var src = new FlatSource { Name = "Test", Age = 50, Value = 3.14 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);

        dest.Name.Should().Be("");  // default
        dest.Age.Should().Be(0);
        dest.Value.Should().Be(0);
    }
}

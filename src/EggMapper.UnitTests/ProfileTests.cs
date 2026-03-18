using System.Reflection;
using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ProfileTests
{
    [Fact]
    public void AddProfile_generic_registers_map_from_profile()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FlatMappingProfile>();
        }).CreateMapper();

        var src = new FlatSource { Name = "Alice", Age = 30 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(30);
    }

    [Fact]
    public void AddProfile_instance_registers_map_from_profile()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new FlatMappingProfile());
        }).CreateMapper();

        var src = new FlatSource { Name = "Bob", Age = 22 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Bob");
    }

    [Fact]
    public void Multiple_profiles_all_registered()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<FlatMappingProfile>();
            cfg.AddProfile<PersonMappingProfile>();
        }).CreateMapper();

        var flatSrc = new FlatSource { Name = "Flat" };
        var flatDest = mapper.Map<FlatSource, FlatDest>(flatSrc);
        flatDest.Name.Should().Be("Flat");

        var personSrc = new PersonSource { Name = "Person", Address = new AddressSource { City = "NYC" } };
        var personDest = mapper.Map<PersonSource, PersonDest>(personSrc);
        personDest.Name.Should().Be("Person");
        personDest.Address!.City.Should().Be("NYC");
    }

    [Fact]
    public void AddProfiles_assembly_scan_finds_profile_types()
    {
        var testAssembly = Assembly.GetExecutingAssembly();
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfiles(new[] { testAssembly });
        }).CreateMapper();

        var src = new FlatSource { Name = "Scanned", Age = 5 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Scanned");
    }

    [Fact]
    public void Profile_name_returns_class_name()
    {
        var profile = new FlatMappingProfile();
        profile.ProfileName.Should().Be(nameof(FlatMappingProfile));
    }
}

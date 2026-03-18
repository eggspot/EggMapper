using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class BasicFlatteningTests
{
    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

    [Fact]
    public void Maps_string_property_by_name()
    {
        var mapper = CreateMapper();
        var src = new FlatSource { Name = "Alice" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Alice");
    }

    [Fact]
    public void Maps_int_property_by_name()
    {
        var mapper = CreateMapper();
        var src = new FlatSource { Age = 30 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(30);
    }

    [Fact]
    public void Maps_double_property_by_name()
    {
        var mapper = CreateMapper();
        var src = new FlatSource { Value = 3.14 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Value.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void Maps_bool_property_by_name()
    {
        var mapper = CreateMapper();
        var src = new FlatSource { IsActive = true };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Maps_all_flat_properties_at_once()
    {
        var mapper = CreateMapper();
        var src = new FlatSource { Name = "Bob", Age = 25, Value = 1.5, Email = "b@b.com", IsActive = true };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Bob");
        dest.Age.Should().Be(25);
        dest.Value.Should().Be(1.5);
        dest.Email.Should().Be("b@b.com");
        dest.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Unmapped_dest_properties_stay_default()
    {
        // Source has only Name; dest has Name + more properties
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Age,      opts => opts.Ignore())
               .ForMember(d => d.Value,    opts => opts.Ignore())
               .ForMember(d => d.Email,    opts => opts.Ignore())
               .ForMember(d => d.IsActive, opts => opts.Ignore());
        }).CreateMapper();

        var src = new FlatSource { Name = "Alice", Age = 99, Value = 3.14 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        // Name is mapped, everything else stays at default
        dest.Name.Should().Be("Alice");
        dest.Age.Should().Be(0);
        dest.Value.Should().Be(0.0);
    }

    [Fact]
    public void Flattened_property_SubName_is_mapped_from_Sub_Name()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlattenSource, FlattenDest>()).CreateMapper();

        var src = new FlattenSource { Sub = new SubObject { Name = "NestedName" }, Value = 7 };
        var dest = mapper.Map<FlattenSource, FlattenDest>(src);
        dest.SubName.Should().Be("NestedName");
        dest.Value.Should().Be(7);
    }

    [Fact]
    public void Flattened_property_SubCount_is_mapped_from_Sub_Count()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlattenSource, FlattenDest>()).CreateMapper();

        var src = new FlattenSource { Sub = new SubObject { Count = 42 } };
        var dest = mapper.Map<FlattenSource, FlattenDest>(src);
        dest.SubCount.Should().Be(42);
    }

    [Fact]
    public void Null_sub_object_results_in_null_flattened_string_property()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlattenSource, FlattenDest>()).CreateMapper();

        var src = new FlattenSource { Sub = null, Value = 1 };
        var dest = mapper.Map<FlattenSource, FlattenDest>(src);
        dest.SubName.Should().BeNull();
    }

    [Fact]
    public void Null_sub_object_leaves_value_type_flattened_property_at_default()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlattenSource, FlattenDest>()).CreateMapper();

        var src = new FlattenSource { Sub = null };
        var dest = mapper.Map<FlattenSource, FlattenDest>(src);
        dest.SubCount.Should().Be(0);
    }

    [Fact]
    public void Int_to_long_type_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<TypeConvSource, TypeConvDest>()).CreateMapper();

        var src = new TypeConvSource { IntVal = 99 };
        var dest = mapper.Map<TypeConvSource, TypeConvDest>(src);
        dest.IntVal.Should().Be(99L);
    }

    [Fact]
    public void Int_to_double_type_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<TypeConvSource, TypeConvDest>()).CreateMapper();

        var src = new TypeConvSource { AnotherInt = 5 };
        var dest = mapper.Map<TypeConvSource, TypeConvDest>(src);
        dest.AnotherInt.Should().Be(5.0);
    }
}

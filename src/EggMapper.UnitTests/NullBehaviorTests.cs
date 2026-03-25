using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class NullBehaviorTests
{
    [Fact]
    public void Null_source_reference_returns_default()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

        var result = mapper.Map<FlatSource, FlatDest>(null!);
        result.Should().BeNull();
    }

    [Fact]
    public void Null_string_source_property_maps_to_null()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

        var src = new FlatSource { Name = null! };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().BeNull();
    }

    [Fact]
    public void Null_nested_object_results_in_null_dest_property()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        }).CreateMapper();

        var src = new PersonSource { Name = "Alice", Address = null };
        var dest = mapper.Map<PersonSource, PersonDest>(src);
        dest.Address.Should().BeNull();
    }

    [Fact]
    public void Null_collection_property_maps_to_empty()
    {
        // Null source collections map to empty destination collections (matches AutoMapper behavior)
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<ItemSource, ItemDest>();
            cfg.CreateMap<CollectionSource, CollectionDest>();
        }).CreateMapper();

        var src = new CollectionSource { Items = null };
        var dest = mapper.Map<CollectionSource, CollectionDest>(src);
        dest.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void NullSubstitute_provides_fallback_when_source_is_null()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts =>
               {
                   opts.MapFrom(s => s.Name);
                   opts.NullSubstitute("N/A");
               });
        }).CreateMapper();

        var src = new FlatSource { Name = null! };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("N/A");
    }

    [Fact]
    public void NullSubstitute_not_applied_when_source_has_value()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts =>
               {
                   opts.MapFrom(s => s.Name);
                   opts.NullSubstitute("N/A");
               });
        }).CreateMapper();

        var src = new FlatSource { Name = "Real" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Real");
    }

    [Fact]
    public void Deep_null_chain_leaves_flattened_string_null()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlattenSource, FlattenDest>()).CreateMapper();

        var src = new FlattenSource { Sub = null };
        var dest = mapper.Map<FlattenSource, FlattenDest>(src);
        dest.SubName.Should().BeNull();
    }

    [Fact]
    public void Deep_null_chain_leaves_flattened_int_at_default()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlattenSource, FlattenDest>()).CreateMapper();

        var src = new FlattenSource { Sub = null };
        var dest = mapper.Map<FlattenSource, FlattenDest>(src);
        dest.SubCount.Should().Be(0);
    }
}

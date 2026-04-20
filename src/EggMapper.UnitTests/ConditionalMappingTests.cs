using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ConditionalMappingTests
{
    [Fact]
    public void Condition_maps_when_true()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Age, opts =>
               {
                   opts.MapFrom(s => s.Age);
                   opts.Condition(s => s.Age > 0);
               });
        }).CreateMapper();

        var src = new FlatSource { Age = 25 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(25);
    }

    [Fact]
    public void Condition_skips_when_false_leaves_default()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Age, opts =>
               {
                   opts.MapFrom(s => s.Age);
                   opts.Condition(s => s.Age > 0);
               });
        }).CreateMapper();

        var src = new FlatSource { Age = -5 };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(0);
    }

    [Fact]
    public void Condition_with_src_and_dest_params_maps_when_true()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts =>
               {
                   opts.MapFrom(s => s.Name);
                   opts.Condition((src, dest) => src.IsActive);
               });
        }).CreateMapper();

        var src = new FlatSource { Name = "Active", IsActive = true };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("Active");
    }

    [Fact]
    public void Condition_with_src_and_dest_params_skips_when_false()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts =>
               {
                   opts.MapFrom(s => s.Name);
                   opts.Condition((src, dest) => src.IsActive);
               });
        }).CreateMapper();

        var src = new FlatSource { Name = "Inactive", IsActive = false };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().BeEmpty();
    }

    [Fact]
    public void PreCondition_skips_when_false()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Value, opts =>
               {
                   opts.MapFrom(s => s.Value);
                   opts.PreCondition(s => s.IsActive);
               });
        }).CreateMapper();

        var src = new FlatSource { Value = 99.9, IsActive = false };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Value.Should().Be(0.0);
    }

    [Fact]
    public void PreCondition_maps_when_true()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Value, opts =>
               {
                   opts.MapFrom(s => s.Value);
                   opts.PreCondition(s => s.IsActive);
               });
        }).CreateMapper();

        var src = new FlatSource { Value = 3.14, IsActive = true };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Value.Should().BeApproximately(3.14, 0.001);
    }

    [Fact]
    public void Condition_without_MapFrom_still_maps_by_convention()
    {
        // ForMember with only Condition (no MapFrom) must still apply the convention
        // source-member lookup — otherwise the property is silently dropped.
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts => opts.Condition(s => !string.IsNullOrWhiteSpace(s.Name)));
        }).CreateMapper();

        var src = new FlatSource { Name = "updated" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("updated");
    }

    [Fact]
    public void Condition_without_MapFrom_skips_when_false()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts => opts.Condition(s => !string.IsNullOrWhiteSpace(s.Name)));
        }).CreateMapper();

        var src = new FlatSource { Name = "" };
        var dest = new FlatDest { Name = "original" };
        var result = mapper.Map(src, dest);
        result.Name.Should().Be("original");
    }

    [Fact]
    public void Condition_without_MapFrom_maps_into_existing_destination()
    {
        // Regression: Map(source, destination) with Condition-only ForMember was silently
        // skipping the property because SourceMemberName was null and convention fallback
        // was blocked by processedDestProps.
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts => opts.Condition(s => !string.IsNullOrWhiteSpace(s.Name)));
        }).CreateMapper();

        var src = new FlatSource { Name = "new name" };
        var dest = new FlatDest { Name = "old name" };
        mapper.Map(src, dest);
        dest.Name.Should().Be("new name");
    }
}

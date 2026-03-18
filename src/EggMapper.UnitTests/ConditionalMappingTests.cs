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
}

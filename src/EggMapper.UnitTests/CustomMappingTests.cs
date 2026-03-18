using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class SourceWithExtra
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public int Score { get; set; }
    public string? Tag { get; set; }
}

file class DestWithExtra
{
    public string FullName { get; set; } = "";
    public int Score { get; set; }
    public string? Tag { get; set; }
    public string? Fixed { get; set; }
}

public class CustomMappingTests
{
    [Fact]
    public void ForMember_MapFrom_expression_maps_property()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName, opts => opts.MapFrom(s => s.FirstName + " " + s.LastName))
               .ForMember(d => d.Tag, opts => opts.Ignore())
               .ForMember(d => d.Fixed, opts => opts.Ignore());
        }).CreateMapper();

        var src = new SourceWithExtra { FirstName = "John", LastName = "Doe" };
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.FullName.Should().Be("John Doe");
    }

    [Fact]
    public void ForMember_MapFrom_lambda_with_two_params()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName, opts =>
                   opts.MapFrom<string>((src, dest) => $"{src.FirstName} ({dest.Score})"))
               .ForMember(d => d.Tag, opts => opts.Ignore())
               .ForMember(d => d.Fixed, opts => opts.Ignore());
        }).CreateMapper();

        var src = new SourceWithExtra { FirstName = "Jane", Score = 10 };
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.FullName.Should().Be("Jane (0)");
    }

    [Fact]
    public void Multiple_ForMember_configurations_all_applied()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName, opts => opts.MapFrom(s => s.FirstName))
               .ForMember(d => d.Tag, opts => opts.MapFrom(s => s.Tag ?? "none"))
               .ForMember(d => d.Fixed, opts => opts.UseValue("constant"));
        }).CreateMapper();

        var src = new SourceWithExtra { FirstName = "X", Tag = null };
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.FullName.Should().Be("X");
        dest.Tag.Should().Be("none");
        dest.Fixed.Should().Be("constant");
    }

    [Fact]
    public void Ignore_leaves_property_at_default()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName, opts => opts.Ignore())
               .ForMember(d => d.Tag, opts => opts.Ignore())
               .ForMember(d => d.Fixed, opts => opts.Ignore());
        }).CreateMapper();

        var src = new SourceWithExtra { FirstName = "Ignored" };
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.FullName.Should().BeEmpty();
    }

    [Fact]
    public void UseValue_sets_fixed_constant_value()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName, opts => opts.Ignore())
               .ForMember(d => d.Tag, opts => opts.Ignore())
               .ForMember(d => d.Fixed, opts => opts.UseValue("FIXED"));
        }).CreateMapper();

        var src = new SourceWithExtra();
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.Fixed.Should().Be("FIXED");
    }

    [Fact]
    public void UseValue_int_sets_fixed_numeric_value()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName, opts => opts.Ignore())
               .ForMember(d => d.Tag, opts => opts.Ignore())
               .ForMember(d => d.Fixed, opts => opts.Ignore())
               .ForMember(d => d.Score, opts => opts.UseValue(999));
        }).CreateMapper();

        var src = new SourceWithExtra { Score = 1 };
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.Score.Should().Be(999);
    }

    [Fact]
    public void MapFrom_complex_expression_concatenates_strings()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithExtra, DestWithExtra>()
               .ForMember(d => d.FullName,
                   opts => opts.MapFrom(s => $"{s.LastName}, {s.FirstName} [{s.Score}]"))
               .ForMember(d => d.Tag, opts => opts.Ignore())
               .ForMember(d => d.Fixed, opts => opts.Ignore());
        }).CreateMapper();

        var src = new SourceWithExtra { FirstName = "Jane", LastName = "Smith", Score = 42 };
        var dest = mapper.Map<SourceWithExtra, DestWithExtra>(src);
        dest.FullName.Should().Be("Smith, Jane [42]");
    }

    [Fact]
    public void ForMember_overrides_convention_mapping()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts => opts.MapFrom(s => "OVERRIDE"));
        }).CreateMapper();

        var src = new FlatSource { Name = "Original" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("OVERRIDE");
    }

    [Fact]
    public void ForAllMembers_applies_option_to_all_writable_members()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForAllMembers(opts => opts.Ignore());
        }).CreateMapper();

        var src = new FlatSource { Name = "Alice", Age = 30, Value = 1.5, Email = "a@a.com", IsActive = true };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().BeEmpty();
        dest.Age.Should().Be(0);
    }

    [Fact]
    public void MapFrom_with_null_returning_expression()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<FlatSource, FlatDest>()
               .ForMember(d => d.Name, opts => opts.MapFrom(s => (string?)null));
        }).CreateMapper();

        var src = new FlatSource { Name = "Alice" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().BeNull();
    }
}

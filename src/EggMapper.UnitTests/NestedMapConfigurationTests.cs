using EggMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class NestedMapConfigurationTests
{
    public class Inner { public string Value { get; set; } = ""; }
    public class InnerDto { public string Value { get; set; } = ""; }

    public class Outer
    {
        public Inner? Inner { get; set; }
        public List<Inner> Items { get; set; } = new();
    }

    public class OuterDto
    {
        public InnerDto? Inner { get; set; }
        public List<InnerDto> Items { get; set; } = new();
    }

    public class UpperCaseResolver : IMemberValueResolver<object, object, string, string>
    {
        public string Resolve(object source, object destination, string sourceMember, string destMember, ResolutionContext context)
            => sourceMember.ToUpperInvariant();
    }

    private static Outer Sample() => new()
    {
        Inner = new Inner { Value = "nested" },
        Items = new List<Inner> { new() { Value = "element" } }
    };

    [Fact]
    public void Map_ChildPairWithConvertUsing_ConvertsNestedMemberAndListElements()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Outer, OuterDto>();
            cfg.CreateMap<Inner, InnerDto>().ConvertUsing(i => new InnerDto { Value = "converted:" + i.Value });
        }).CreateMapper();

        var result = mapper.Map<Outer, OuterDto>(Sample());

        result.Inner!.Value.Should().Be("converted:nested");
        result.Items.Single().Value.Should().Be("converted:element");
    }

    [Fact]
    public void MapList_PairWithConvertUsing_UsesTheConverter()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<Inner, InnerDto>().ConvertUsing(i => new InnerDto { Value = "converted:" + i.Value })).CreateMapper();

        var result = mapper.MapList<Inner, InnerDto>(new List<Inner> { new() { Value = "a" } });

        result.Single().Value.Should().Be("converted:a");
    }

    [Fact]
    public void Map_ChildPairWithContextAfterMap_RunsItForNestedMemberAndListElements()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Outer, OuterDto>();
            cfg.CreateMap<Inner, InnerDto>().AfterMap((s, d, ctx) => d.Value += "!");
        }).CreateMapper();

        var result = mapper.Map<Outer, OuterDto>(Sample());

        result.Inner!.Value.Should().Be("nested!");
        result.Items.Single().Value.Should().Be("element!");
    }

    [Fact]
    public void Map_ChildPairWithValidationRule_ThrowsForInvalidNestedValue()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Outer, OuterDto>();
            cfg.CreateMap<Inner, InnerDto>().Validate(d => d.Value, v => v.Length > 0, "Value is required");
        }).CreateMapper();
        var source = new Outer { Inner = new Inner { Value = "" } };

        var act = () => mapper.Map<Outer, OuterDto>(source);

        act.Should().Throw<Exception>().Where(e => e.ToString().Contains("Value is required"));
    }

    [Fact]
    public void Map_ChildMemberWithDependencyInjectionResolver_ResolvesNestedMemberAndListElements()
    {
        var services = new ServiceCollection();
        services.AddEggMapper(cfg =>
        {
            cfg.CreateMap<Outer, OuterDto>();
            cfg.CreateMap<Inner, InnerDto>()
               .ForMember(d => d.Value, o => o.MapFrom<UpperCaseResolver, string>(s => s.Value));
        });
        using var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        var result = mapper.Map<Outer, OuterDto>(Sample());

        result.Inner!.Value.Should().Be("NESTED");
        result.Items.Single().Value.Should().Be("ELEMENT");
    }

    [Fact]
    public void Map_ChildPairWithoutSpecialConfiguration_StillMapsByName()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Outer, OuterDto>();
            cfg.CreateMap<Inner, InnerDto>();
        }).CreateMapper();

        var result = mapper.Map<Outer, OuterDto>(Sample());

        result.Inner!.Value.Should().Be("nested");
        result.Items.Single().Value.Should().Be("element");
    }
}

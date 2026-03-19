using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class AfterMapContextTests
{
    [Fact]
    public void AfterMap_WithContext_CanAccessMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AmcInner, AmcInnerDto>();
            cfg.CreateMap<AmcSource, AmcDest>()
               .ForMember(d => d.Inner, o => o.Ignore())
               .AfterMap((s, d, ctx) =>
               {
                   d.Inner = ctx.Mapper.Map<AmcInner, AmcInnerDto>(s.Inner);
               });
        });
        var mapper = config.CreateMapper();

        var src = new AmcSource
        {
            Name = "Parent",
            Inner = new AmcInner { Value = 42 }
        };

        var result = mapper.Map<AmcSource, AmcDest>(src);
        result.Name.Should().Be("Parent");
        result.Inner.Should().NotBeNull();
        result.Inner!.Value.Should().Be(42);
    }

    [Fact]
    public void BeforeMap_WithContext_CanAccessMapper()
    {
        var captured = new List<string>();
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AmcSource, AmcDest>()
               .ForMember(d => d.Inner, o => o.Ignore())
               .BeforeMap((s, d, ctx) =>
               {
                   captured.Add($"Before:{s.Name}");
               });
        });
        var mapper = config.CreateMapper();

        mapper.Map<AmcSource, AmcDest>(new AmcSource { Name = "Test", Inner = new AmcInner() });
        captured.Should().Contain("Before:Test");
    }

    [Fact]
    public void AfterMap_WithContext_CanMapCollections()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AmcItem, AmcItemDto>();
            cfg.CreateMap<AmcParent, AmcParentDto>()
               .ForMember(d => d.Items, o => o.Ignore())
               .AfterMap((s, d, ctx) =>
               {
                   d.Items = s.Items.Select(i => ctx.Mapper.Map<AmcItem, AmcItemDto>(i)).ToList();
               });
        });
        var mapper = config.CreateMapper();

        var src = new AmcParent
        {
            Items = new List<AmcItem>
            {
                new() { Name = "A" },
                new() { Name = "B" }
            }
        };

        var result = mapper.Map<AmcParent, AmcParentDto>(src);
        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("A");
    }
}

file class AmcInner { public int Value { get; set; } }
file class AmcInnerDto { public int Value { get; set; } }
file class AmcSource { public string Name { get; set; } = ""; public AmcInner Inner { get; set; } = new(); }
file class AmcDest { public string Name { get; set; } = ""; public AmcInnerDto? Inner { get; set; } }
file class AmcItem { public string Name { get; set; } = ""; }
file class AmcItemDto { public string Name { get; set; } = ""; }
file class AmcParent { public List<AmcItem> Items { get; set; } = new(); }
file class AmcParentDto { public List<AmcItemDto> Items { get; set; } = new(); }

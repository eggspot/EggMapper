using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Tests for 4-argument MapFrom: (src, dest, destMember, context) =>
/// where context provides access to the mapper for recursive mapping.
/// </summary>
public class MapFromContextTests
{
    [Fact]
    public void MapFrom_4Arg_CanAccessMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MfcInner, MfcInnerDto>();
            cfg.CreateMap<MfcSource, MfcDest>()
               .ForMember(d => d.Inner, o => o.MapFrom((s, d, member, ctx) =>
                   ctx.Mapper.Map<MfcInner, MfcInnerDto>(s.Inner)));
        });
        var mapper = config.CreateMapper();

        var src = new MfcSource { Inner = new MfcInner { Value = 99 } };
        var result = mapper.Map<MfcSource, MfcDest>(src);
        result.Inner.Should().NotBeNull();
        result.Inner!.Value.Should().Be(99);
    }

    [Fact]
    public void MapFrom_4Arg_CanMapCollectionWithGroupBy()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<MfcItem, MfcItemDto>();
            cfg.CreateMap<MfcListSource, MfcGroupedDest>()
               .ForMember(d => d.GroupedItems, o => o.MapFrom((s, d, member, ctx) =>
                   s.Items.GroupBy(i => i.Category).ToDictionary(
                       g => g.Key,
                       g => ctx.Mapper.MapList<MfcItem, MfcItemDto>(g.ToList()))));
        });
        var mapper = config.CreateMapper();

        var src = new MfcListSource
        {
            Items = new List<MfcItem>
            {
                new() { Category = "A", Name = "A1" },
                new() { Category = "A", Name = "A2" },
                new() { Category = "B", Name = "B1" }
            }
        };

        var result = mapper.Map<MfcListSource, MfcGroupedDest>(src);
        result.GroupedItems.Should().ContainKey("A");
        result.GroupedItems["A"].Should().HaveCount(2);
        result.GroupedItems["B"].Should().HaveCount(1);
    }

    [Fact]
    public void MapFrom_3Arg_WithDestMember()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<MfcSimpleSrc, MfcSimpleDst>()
               .ForMember(d => d.FullName, o => o.MapFrom((s, d, member) =>
                   $"{s.First} {s.Last}")));
        var mapper = config.CreateMapper();

        var result = mapper.Map<MfcSimpleSrc, MfcSimpleDst>(
            new MfcSimpleSrc { First = "John", Last = "Doe" });
        result.FullName.Should().Be("John Doe");
    }
}

file class MfcInner { public int Value { get; set; } }
file class MfcInnerDto { public int Value { get; set; } }
file class MfcSource { public MfcInner Inner { get; set; } = new(); }
file class MfcDest { public MfcInnerDto? Inner { get; set; } }
file class MfcItem { public string Category { get; set; } = ""; public string Name { get; set; } = ""; }
file class MfcItemDto { public string Name { get; set; } = ""; }
file class MfcListSource { public List<MfcItem> Items { get; set; } = new(); }
file class MfcGroupedDest { public Dictionary<string, List<MfcItemDto>> GroupedItems { get; set; } = new(); }
file class MfcSimpleSrc { public string First { get; set; } = ""; public string Last { get; set; } = ""; }
file class MfcSimpleDst { public string FullName { get; set; } = ""; }

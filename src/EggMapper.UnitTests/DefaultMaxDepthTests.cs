using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Tests for the global DefaultMaxDepth safety net (Feature 3).
/// DefaultMaxDepth only applies to the flexible delegate path (maps with hooks,
/// conditions, inheritance, etc.) — not to the ctx-free or typed paths.
/// Individual per-map MaxDepth is covered by MaxDepthTests.cs.
/// </summary>
public class DefaultMaxDepthTests
{
    private static NodeSrc BuildChain(int levels)
    {
        var root = new NodeSrc { Name = "L0" };
        var current = root;
        for (int i = 1; i < levels; i++)
        {
            var next = new NodeSrc { Name = $"L{i}" };
            current.Child = next;
            current = next;
        }
        return root;
    }

    [Fact]
    public void DefaultMaxDepth_32_is_default()
    {
        // Verify the default DefaultMaxDepth value is 32
        int captured = 0;
        _ = new MapperConfiguration(cfg =>
        {
            captured = cfg.DefaultMaxDepth;
            cfg.CreateMap<NodeSrc, NodeDst>();
        });
        captured.Should().Be(32);
    }

    [Fact]
    public void DefaultMaxDepth_applies_to_flexible_path_maps()
    {
        // Maps with hooks use the flexible delegate path — DefaultMaxDepth applies there.
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.DefaultMaxDepth = 2;
            cfg.CreateMap<NodeSrc, NodeDst>()
               .BeforeMap((src, dest) => { }); // Forces flexible delegate path
        }).CreateMapper();

        var src = BuildChain(5); // L0 → L1 → L2 → L3 → L4
        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        dest.Name.Should().Be("L0");
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().Be("L1");
        dest.Child.Child.Should().BeNull(); // DefaultMaxDepth=2 cuts at depth 2
    }

    [Fact]
    public void DefaultMaxDepth_1_stops_at_root_in_flexible_path()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.DefaultMaxDepth = 1;
            cfg.CreateMap<NodeSrc, NodeDst>()
               .BeforeMap((src, dest) => { });
        }).CreateMapper();

        var src = BuildChain(3);
        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        dest.Name.Should().Be("L0");
        dest.Child.Should().BeNull();
    }

    [Fact]
    public void DefaultMaxDepth_0_disables_safety_net()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.DefaultMaxDepth = 0;
            cfg.CreateMap<NodeSrc, NodeDst>()
               .BeforeMap((src, dest) => { });
        }).CreateMapper();

        var src = BuildChain(5);
        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        // All 5 levels mapped when safety net is disabled
        dest.Child!.Child!.Child!.Child!.Name.Should().Be("L4");
    }

    [Fact]
    public void PerMap_MaxDepth_overrides_DefaultMaxDepth()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.DefaultMaxDepth = 10;
            cfg.CreateMap<NodeSrc, NodeDst>().MaxDepth(2); // per-map MaxDepth wins
        }).CreateMapper();

        var src = BuildChain(5);
        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        dest.Child.Should().NotBeNull();
        dest.Child!.Child.Should().BeNull(); // MaxDepth(2) cuts at depth 2
    }

    [Fact]
    public void DefaultMaxDepth_is_configurable()
    {
        int captured = 0;
        _ = new MapperConfiguration(cfg =>
        {
            cfg.DefaultMaxDepth = 64;
            captured = cfg.DefaultMaxDepth;
            cfg.CreateMap<NodeSrc, NodeDst>();
        });
        captured.Should().Be(64);
    }
}

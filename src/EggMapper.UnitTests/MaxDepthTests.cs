using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

// MaxDepth reuses NodeSrc/NodeDst from CircularReferenceTests.

public class MaxDepthTests
{
    private static IMapper CreateTreeMapper(int maxDepth) =>
        new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NodeSrc, NodeDst>().MaxDepth(maxDepth);
        }).CreateMapper();

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
    public void MaxDepth_1_maps_root_only_child_is_null()
    {
        var mapper = CreateTreeMapper(1);
        var src = BuildChain(3); // L0 -> L1 -> L2

        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        dest.Name.Should().Be("L0");
        // MaxDepth(1): root at depth 0 is mapped, child at depth 1 is cut off → null
        dest.Child.Should().BeNull();
    }

    [Fact]
    public void MaxDepth_2_maps_two_levels()
    {
        var mapper = CreateTreeMapper(2);
        var src = BuildChain(3); // L0 -> L1 -> L2

        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        dest.Name.Should().Be("L0");
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().Be("L1");
        // Third level not mapped (depth limit)
        dest.Child.Child.Should().BeNull();
    }

    [Fact]
    public void No_MaxDepth_maps_all_levels_in_finite_chain()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NodeSrc, NodeDst>();
        }).CreateMapper();

        var src = BuildChain(3);
        var dest = mapper.Map<NodeSrc, NodeDst>(src);
        dest.Name.Should().Be("L0");
        dest.Child!.Name.Should().Be("L1");
        dest.Child.Child!.Name.Should().Be("L2");
    }
}

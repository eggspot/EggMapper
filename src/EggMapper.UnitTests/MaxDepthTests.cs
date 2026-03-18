using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

// MaxDepth requires DIFFERENT source/dest types so the recursive call
// goes through the TypeMap delegate (which checks depth), rather than
// direct assignment (which bypasses the TypeMap entirely).
internal class NodeSrc { public string Name { get; set; } = ""; public NodeSrc? Child { get; set; } }
internal class NodeDst { public string Name { get; set; } = ""; public NodeDst? Child { get; set; } }

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
    public void MaxDepth_1_maps_root_name_but_not_child_name()
    {
        var mapper = CreateTreeMapper(1);
        var src = BuildChain(3); // L0 -> L1 -> L2

        var dest = mapper.Map<NodeSrc, NodeDst>(src);

        dest.Name.Should().Be("L0");
        // Child node is created but its Name is not mapped (depth limit reached)
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().BeEmpty();
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
        dest.Child.Child.Should().NotBeNull();
        dest.Child.Child!.Name.Should().BeEmpty();
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

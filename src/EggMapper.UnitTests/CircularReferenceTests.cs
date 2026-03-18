using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class CircularReferenceTests
{
    [Fact]
    public void MaxDepth_CircularReference_StopsAtDepth()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<TreeNode, TreeNode>().MaxDepth(3));
        var mapper = config.CreateMapper();

        // Build a chain: A -> B -> C -> D -> E
        var root = new TreeNode
        {
            Name = "A",
            Child = new TreeNode
            {
                Name = "B",
                Child = new TreeNode
                {
                    Name = "C",
                    Child = new TreeNode
                    {
                        Name = "D",
                        Child = new TreeNode { Name = "E" }
                    }
                }
            }
        };

        var dest = mapper.Map<TreeNode, TreeNode>(root);

        dest.Name.Should().Be("A");
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().Be("B");
        dest.Child.Child.Should().NotBeNull();
        dest.Child.Child!.Name.Should().Be("C");
        // MaxDepth 3 means depth 0,1,2 are mapped; depth 3 is cut off
        dest.Child.Child.Child.Should().BeNull();
    }

    [Fact]
    public void MaxDepth_1_OnlyCopiesRootProperties()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<TreeNode, TreeNode>().MaxDepth(1));
        var mapper = config.CreateMapper();

        var root = new TreeNode
        {
            Name = "Root",
            Child = new TreeNode { Name = "Child1", Child = new TreeNode { Name = "Child2" } }
        };

        var dest = mapper.Map<TreeNode, TreeNode>(root);
        dest.Name.Should().Be("Root");
        dest.Child.Should().BeNull();
    }

    [Fact]
    public void MaxDepth_SeparateSourceAndDest_StopsAtDepth()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<NodeSrc, NodeDst>().MaxDepth(2);
        });
        var mapper = config.CreateMapper();

        var root = new NodeSrc
        {
            Name = "Root",
            Child = new NodeSrc
            {
                Name = "Level1",
                Child = new NodeSrc
                {
                    Name = "Level2",
                    Child = new NodeSrc { Name = "Level3" }
                }
            }
        };

        var dest = mapper.Map<NodeSrc, NodeDst>(root);
        dest.Name.Should().Be("Root");
        dest.Child.Should().NotBeNull();
        dest.Child!.Name.Should().Be("Level1");
        dest.Child.Child.Should().BeNull();
    }
}

public class NodeSrc
{
    public string Name { get; set; } = "";
    public NodeSrc? Child { get; set; }
}

public class NodeDst
{
    public string Name { get; set; } = "";
    public NodeDst? Child { get; set; }
}

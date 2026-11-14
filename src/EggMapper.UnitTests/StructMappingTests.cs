using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class StructMappingTests
{
    public struct SmPointSource
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct SmPointDest
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class SmPointClass
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct SmCtorDest
    {
        public SmCtorDest() { Label = "constructed"; }
        public int X { get; set; }
        public string Label { get; set; }
    }

    public class SmShape
    {
        public string Name { get; set; } = "";
        public SmPointSource Origin { get; set; }
        public List<SmPointSource> Points { get; set; } = new();
    }

    public class SmShapeDto
    {
        public string Name { get; set; } = "";
        public SmPointDest Origin { get; set; }
        public List<SmPointDest> Points { get; set; } = new();
    }

    [Fact]
    public void Map_StructToStruct_CopiesProperties()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointSource, SmPointDest>()).CreateMapper();

        var result = mapper.Map<SmPointSource, SmPointDest>(new SmPointSource { X = 1, Y = 2 });

        result.X.Should().Be(1);
        result.Y.Should().Be(2);
    }

    [Fact]
    public void Map_ClassToStruct_CopiesProperties()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointClass, SmPointDest>()).CreateMapper();

        var result = mapper.Map<SmPointClass, SmPointDest>(new SmPointClass { X = 3, Y = 4 });

        result.X.Should().Be(3);
        result.Y.Should().Be(4);
    }

    [Fact]
    public void Map_StructToClass_CopiesProperties()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointSource, SmPointClass>()).CreateMapper();

        var result = mapper.Map<SmPointSource, SmPointClass>(new SmPointSource { X = 5, Y = 6 });

        result.X.Should().Be(5);
        result.Y.Should().Be(6);
    }

    [Fact]
    public void Map_StructDestinationWithAfterMap_CopiesPropertiesAndRunsAfterMap()
    {
        var afterMapRuns = 0;
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointSource, SmPointDest>()
            .AfterMap((s, d) => afterMapRuns++)).CreateMapper();

        var result = mapper.Map<SmPointSource, SmPointDest>(new SmPointSource { X = 7, Y = 8 });

        result.X.Should().Be(7);
        result.Y.Should().Be(8);
        afterMapRuns.Should().Be(1);
    }

    [Fact]
    public void Map_StructDestinationWithExplicitParameterlessConstructor_RunsConstructor()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointSource, SmCtorDest>()).CreateMapper();

        var result = mapper.Map<SmPointSource, SmCtorDest>(new SmPointSource { X = 9 });

        result.X.Should().Be(9);
        result.Label.Should().Be("constructed");
    }

    [Fact]
    public void Map_ClassWithNestedStructAndStructList_MapsAll()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SmPointSource, SmPointDest>();
            cfg.CreateMap<SmShape, SmShapeDto>();
        }).CreateMapper();
        var shape = new SmShape
        {
            Name = "tri",
            Origin = new SmPointSource { X = 1, Y = 1 },
            Points = { new SmPointSource { X = 2, Y = 3 }, new SmPointSource { X = 4, Y = 5 } }
        };

        var result = mapper.Map<SmShape, SmShapeDto>(shape);

        result.Name.Should().Be("tri");
        result.Origin.X.Should().Be(1);
        result.Points.Select(p => (p.X, p.Y)).Should().Equal((2, 3), (4, 5));
    }

    [Fact]
    public void MapList_Structs_MapsEveryElement()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointSource, SmPointDest>()).CreateMapper();

        var result = mapper.MapList<SmPointSource, SmPointDest>(new[] { new SmPointSource { X = 1 }, new SmPointSource { X = 2 } });

        result.Select(p => p.X).Should().Equal(1, 2);
    }

    [Fact]
    public void Map_BoxedStructSource_CopiesProperties()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<SmPointSource, SmPointDest>()).CreateMapper();
        object boxed = new SmPointSource { X = 11, Y = 12 };

        var result = mapper.Map<SmPointDest>(boxed);

        result.X.Should().Be(11);
        result.Y.Should().Be(12);
    }
}

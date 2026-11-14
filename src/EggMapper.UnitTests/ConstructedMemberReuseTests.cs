using EggMapper;
using EggMapper.Internal;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ConstructedMemberReuseTests
{
    public class RuChildSource
    {
        public string City { get; set; } = "";
        public int Zip { get; set; }
    }

    public class RuChild
    {
        public string City { get; set; } = "";
        public int Zip { get; set; }
        public string Tag { get; set; } = "";
    }

    public class RuSource
    {
        public string Name { get; set; } = "";
        public RuChildSource? Child { get; set; }
        public List<RuChildSource>? Items { get; set; }
    }

    public class RuDest
    {
        public string Name { get; set; } = string.Empty;
        public RuChild Child { get; set; } = new();
        public List<RuChild> Items { get; set; } = new();
    }

    public class RuTwinDest
    {
        public string Name { get; set; } = string.Empty;
        public RuChild? Child { get; set; }
        public List<RuChild>? Items { get; set; }
    }

    public class RuCollectionExpressionDest
    {
        public List<RuChild> Items { get; set; } = [];
    }

    public class RuSharedChildDest
    {
        public static readonly RuChild SharedChild = new();
        public RuChild Child { get; set; } = SharedChild;
    }

    public class RuExplicitBodyDest
    {
        public RuExplicitBodyDest() { Child = new RuChild(); }
        public RuChild Child { get; set; }
    }

    public class RuComputedPropertyDest
    {
        public RuChild Child { get; set; } = new();
        public string Label => "computed";
    }

    public class RuNoisyChild
    {
        public static int Constructed;
        public RuNoisyChild() { Constructed++; }
        public string City { get; set; } = "";
    }

    public class RuNoisyChildDest
    {
        public RuNoisyChild Child { get; set; } = new();
    }

    public class RuFinalizedChild
    {
        ~RuFinalizedChild() { }
        public string City { get; set; } = "";
    }

    public class RuFinalizedChildDest
    {
        public RuFinalizedChild Child { get; set; } = new();
    }

    public class RuGrandchild
    {
        public RuGrandchild() { RuNoisyChild.Constructed++; }
    }

    public class RuChildWithNoisyGrandchild
    {
        public RuGrandchild Grandchild { get; set; } = new();
    }

    public class RuTransitiveDest
    {
        public RuChildWithNoisyGrandchild Child { get; set; } = new();
    }

    public class RuDerivedChild : RuChild
    {
    }

    public class RuDerivedInitializerDest
    {
        public RuChild Child { get; set; } = new RuDerivedChild();
    }

    public class RuGenericDest<T>
    {
        public List<T> Items { get; set; } = new();
    }

    public record RuRecordDest
    {
        public RuChild Child { get; init; } = new();
    }

    private static IReadOnlyDictionary<System.Reflection.PropertyInfo, Type> Analyze<T>()
        => ConstructorInitializers.GetPropertiesInitializedWithNewInstance(typeof(T));

    private static IEnumerable<string> AnalyzedNames<T>() => Analyze<T>().Keys.Select(p => p.Name);

    [Fact]
    public void Analyze_AutoPropertyAndListInitializers_ReturnsBoth()
    {
        AnalyzedNames<RuDest>().Should().BeEquivalentTo("Child", "Items");
        Analyze<RuDest>().Single(e => e.Key.Name == "Items").Value.Should().Be(typeof(List<RuChild>));
    }

    [Fact]
    public void Analyze_CollectionExpressionOnList_ReturnsMember()
    {
        AnalyzedNames<RuCollectionExpressionDest>().Should().Contain("Items");
    }

    [Fact]
    public void Analyze_SharedStaticInstance_ReturnsNothing()
    {
        AnalyzedNames<RuSharedChildDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ExplicitConstructorBodyAssignment_ReturnsNothing()
    {
        AnalyzedNames<RuExplicitBodyDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ComputedProperty_ReturnsNothing()
    {
        AnalyzedNames<RuComputedPropertyDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ChildConstructorWithSideEffect_ReturnsNothing()
    {
        AnalyzedNames<RuNoisyChildDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ChildWithFinalizer_ReturnsNothing()
    {
        AnalyzedNames<RuFinalizedChildDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_GrandchildConstructorWithSideEffect_ReturnsNothing()
    {
        AnalyzedNames<RuTransitiveDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_InitializerOfDerivedType_ReturnsNothing()
    {
        AnalyzedNames<RuDerivedInitializerDest>().Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ClosedGenericDestination_ReturnsListMember()
    {
        AnalyzedNames<RuGenericDest<int>>().Should().Contain("Items");
    }

    [Fact]
    public void Analyze_NominalRecordWithInitOnlyMember_ReturnsMember()
    {
        AnalyzedNames<RuRecordDest>().Should().Contain("Child");
    }

    private static MapperConfiguration ReuseConfiguration() => new(cfg =>
    {
        cfg.CreateMap<RuChildSource, RuChild>();
        cfg.CreateMap<RuSource, RuDest>();
        cfg.CreateMap<RuSource, RuTwinDest>();
    });

    private static RuSource Source(int seed) => new()
    {
        Name = "n" + seed,
        Child = new RuChildSource { City = "c" + seed, Zip = seed },
        Items = new List<RuChildSource> { new() { City = "a", Zip = 1 }, new() { City = "b", Zip = 2 } }
    };

    [Fact]
    public void Map_ConstructedMembers_MapsValuesAndRecordsReuse()
    {
        var config = ReuseConfiguration();
        var mapper = config.CreateMapper();

        var result = mapper.Map<RuSource, RuDest>(Source(7));

        result.Name.Should().Be("n7");
        result.Child.City.Should().Be("c7");
        result.Child.Zip.Should().Be(7);
        result.Items.Select(i => i.City).Should().Equal("a", "b");
        config.TypeMaps[new TypePair(typeof(RuSource), typeof(RuDest))].ReusedConstructorMembers
            .Should().BeEquivalentTo("Child", "Items");
    }

    [Fact]
    public void Map_IntoExistingDestination_LeavesExistingChildUntouched()
    {
        var mapper = ReuseConfiguration().CreateMapper();
        var sharedChild = new RuChild { City = "shared" };
        var existing = new RuDest { Child = sharedChild };

        mapper.Map(Source(3), existing);

        sharedChild.City.Should().Be("shared");
        existing.Child.Should().NotBeSameAs(sharedChild);
        existing.Child.City.Should().Be("c3");
    }

    [Fact]
    public void Map_NullSourceMembers_KeepConstructedInstances()
    {
        var mapper = ReuseConfiguration().CreateMapper();

        var result = mapper.Map<RuSource, RuDest>(new RuSource { Name = "x" });

        result.Child.Should().NotBeNull();
        result.Child.City.Should().BeEmpty();
        result.Items.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Map_RepeatedCalls_ProduceIndependentInstances()
    {
        var mapper = ReuseConfiguration().CreateMapper();

        var first = mapper.Map<RuSource, RuDest>(Source(1));
        var second = mapper.Map<RuSource, RuDest>(Source(2));

        first.Child.Should().NotBeSameAs(second.Child);
        first.Items.Should().NotBeSameAs(second.Items);
        first.Child.City.Should().Be("c1");
        second.Child.City.Should().Be("c2");
    }

    [Fact]
    public void MapList_ElementsWithConstructedMembers_MapsEachElementIndependently()
    {
        var config = ReuseConfiguration();
        var mapper = config.CreateMapper();

        var result = mapper.MapList<RuSource, RuDest>(new List<RuSource> { Source(1), Source(2), new() { Name = "empty" } });

        result.Select(d => d.Child.City).Should().Equal("c1", "c2", "");
        result[0].Child.Should().NotBeSameAs(result[1].Child);
        result[2].Items.Should().BeEmpty();
        config.TypeMaps[new TypePair(typeof(RuSource), typeof(RuDest))].ReusedConstructorMembers
            .Should().Contain("Child");
    }

    [Fact]
    public void Map_ChildMapWithConstructUsing_StillUsesTheCustomConstructor()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<RuChildSource, RuChild>().ConstructUsing(s => new RuChild { Tag = "custom" });
            cfg.CreateMap<RuSource, RuDest>();
        });

        var result = config.CreateMapper().Map<RuSource, RuDest>(Source(5));

        result.Child.Tag.Should().Be("custom");
        result.Child.City.Should().Be("c5");
    }

    [Fact]
    public void Map_ConstructedMembers_AllocateNoMoreThanDestinationWithoutInitializers()
    {
        var mapper = ReuseConfiguration().CreateMapper();
        var source = Source(9);
        for (var i = 0; i < 100; i++)
        {
            mapper.Map<RuSource, RuDest>(source);
            mapper.Map<RuSource, RuTwinDest>(source);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) mapper.Map<RuSource, RuDest>(source);
        var withInitializers = GC.GetAllocatedBytesForCurrentThread() - before;

        before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++) mapper.Map<RuSource, RuTwinDest>(source);
        var withoutInitializers = GC.GetAllocatedBytesForCurrentThread() - before;

        withInitializers.Should().BeLessThanOrEqualTo(withoutInitializers);
    }
}

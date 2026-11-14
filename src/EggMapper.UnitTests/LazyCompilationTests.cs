using EggMapper;
using EggMapper.Internal;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class LazyCompilationTests
{
    public class LcSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class LcDest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class LcOther
    {
        public string Code { get; set; } = "";
    }

    public class LcOtherDest
    {
        public string Code { get; set; } = "";
    }

    public class LcNode
    {
        public string Name { get; set; } = "";
        public LcNode? Parent { get; set; }
    }

    public class LcNodeDto
    {
        public string Name { get; set; } = "";
        public LcNodeDto? Parent { get; set; }
    }

    public class LcAlpha
    {
        public string Label { get; set; } = "";
        public LcBeta? Beta { get; set; }
    }

    public class LcBeta
    {
        public string Label { get; set; } = "";
        public LcAlpha? Alpha { get; set; }
    }

    public class LcAlphaDto
    {
        public string Label { get; set; } = "";
        public LcBetaDto? Beta { get; set; }
    }

    public class LcBetaDto
    {
        public string Label { get; set; } = "";
        public LcAlphaDto? Alpha { get; set; }
    }

    public class LcBroken
    {
        public string Text { get; set; } = "";
    }

    public class LcBrokenDest
    {
        public string Text { get; set; } = "";
        public string Boom { get; set; } = "";
    }

    public class LcBrokenDest2
    {
        public string Boom { get; set; } = "";
    }

    public class LcParent
    {
        public string Name { get; set; } = "";
        public LcBroken? Child { get; set; }
    }

    public class LcParentDto
    {
        public string Name { get; set; } = "";
        public LcBrokenDest? Child { get; set; }
    }

    public class LcWrapper<T>
    {
        public T Value { get; set; } = default!;
    }

    public class LcWrapperDto<T>
    {
        public T Value { get; set; } = default!;
    }

    public class LcBase
    {
        public string Title { get; set; } = "";
    }

    public class LcDerived : LcBase
    {
        public int Size { get; set; }
    }

    public class LcDerivedProxy : LcDerived
    {
    }

    public class LcBaseDto
    {
        public string Title { get; set; } = "";
    }

    public class LcDerivedDto : LcBaseDto
    {
        public int Size { get; set; }
    }

    public class LcHolder
    {
        public LcSource Item { get; set; } = new();
    }

    private static TypePair Pair<TSource, TDestination>() => new(typeof(TSource), typeof(TDestination));

    private static MapperConfiguration TwoMapConfiguration() => new(cfg =>
    {
        cfg.CreateMap<LcSource, LcDest>();
        cfg.CreateMap<LcOther, LcOtherDest>();
    });

    [Fact]
    public void Constructor_RegisteredMaps_CompilesNothing()
    {
        var config = TwoMapConfiguration();

        config.FrozenMaps.Should().BeEmpty();
        config.FrozenCtxFreeMaps.Should().BeEmpty();
    }

    [Fact]
    public void Map_FirstUse_CompilesOnlyThatPair()
    {
        var config = TwoMapConfiguration();

        config.CreateMapper().Map<LcSource, LcDest>(new LcSource { Id = 1 }).Id.Should().Be(1);

        config.FrozenMaps.Should().ContainKey(Pair<LcSource, LcDest>());
        config.FrozenMaps.Should().NotContainKey(Pair<LcOther, LcOtherDest>());
    }

    [Fact]
    public void MapIntoExisting_FirstUse_CompilesAndMaps()
    {
        var mapper = TwoMapConfiguration().CreateMapper();
        var destination = new LcDest();

        mapper.Map(new LcSource { Id = 4, Name = "four" }, destination).Should().BeSameAs(destination);

        destination.Name.Should().Be("four");
    }

    [Fact]
    public void MapObjectSource_FirstUse_CompilesAndMaps()
    {
        var mapper = TwoMapConfiguration().CreateMapper();

        mapper.Map<LcOtherDest>(new LcOther { Code = "c" }).Code.Should().Be("c");
    }

    [Fact]
    public void CompileMappings_RegisteredMaps_CompilesAll()
    {
        var config = TwoMapConfiguration();

        config.CompileMappings();

        config.FrozenMaps.Keys.Should().BeEquivalentTo(new[] { Pair<LcSource, LcDest>(), Pair<LcOther, LcOtherDest>() });
    }

    [Fact]
    public void CompileMappings_CalledTwice_KeepsCompiledDelegates()
    {
        var config = TwoMapConfiguration();
        config.CompileMappings();
        var first = config.FrozenMaps[Pair<LcSource, LcDest>()];

        config.CompileMappings();

        config.FrozenMaps[Pair<LcSource, LcDest>()].Should().BeSameAs(first);
    }

    [Fact]
    public void MapList_ListOnlyPair_DoesNotCompileItemMap()
    {
        var config = TwoMapConfiguration();

        var result = config.CreateMapper().MapList<LcSource, LcDest>(new List<LcSource> { new() { Id = 1 }, new() { Id = 2 } });

        result.Select(d => d.Id).Should().Equal(1, 2);
        config.FrozenMaps.Should().NotContainKey(Pair<LcSource, LcDest>());
    }

    [Fact]
    public void Patch_PatchOnlyPair_DoesNotCompileMap()
    {
        var config = TwoMapConfiguration();
        var destination = new LcDest { Id = 1, Name = "keep" };

        config.CreateMapper().Patch(new LcSource { Id = 2, Name = null! }, destination);

        destination.Id.Should().Be(2);
        config.FrozenMaps.Should().NotContainKey(Pair<LcSource, LcDest>());
    }

    [Fact]
    public void GetMappingExpressionText_BeforeFirstMap_ReturnsText()
    {
        var config = TwoMapConfiguration();

        config.GetMappingExpressionText<LcSource, LcDest>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Map_NullableValueTypeConvertUsingWithNullSource_FirstUse_UsesConverter()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<int?, string>()
            .ConvertUsing(s => s.HasValue ? s.Value.ToString() : "none")).CreateMapper();

        mapper.Map<int?, string>(null).Should().Be("none");
        mapper.Map<int?, string>(5).Should().Be("5");
    }

    [Fact]
    public void Map_SelfReferencingType_ConcurrentFirstUse_MapsWithoutAliasing()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<LcNode, LcNodeDto>()).CreateMapper();
        var source = new LcNode { Name = "child", Parent = new LcNode { Name = "root" } };
        var failures = 0;

        Parallel.For(0, 64, _ =>
        {
            var dto = mapper.Map<LcNode, LcNodeDto>(source);
            if (dto.Name != "child" || dto.Parent is not { Name: "root", Parent: null })
                Interlocked.Increment(ref failures);
        });

        failures.Should().Be(0);
    }

    [Fact]
    public void Map_MutuallyRecursiveTypes_ConcurrentFirstUse_MapsWithoutAliasing()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<LcAlpha, LcAlphaDto>();
            cfg.CreateMap<LcBeta, LcBetaDto>();
        }).CreateMapper();
        var source = new LcAlpha { Label = "a", Beta = new LcBeta { Label = "b", Alpha = new LcAlpha { Label = "a2" } } };
        var failures = 0;

        Parallel.For(0, 64, i =>
        {
            if (i % 2 == 0)
            {
                var dto = mapper.Map<LcAlpha, LcAlphaDto>(source);
                if (dto.Beta is not { Label: "b" } || dto.Beta.Alpha is not { Label: "a2" })
                    Interlocked.Increment(ref failures);
            }
            else
            {
                var dto = mapper.Map<LcBeta, LcBetaDto>(source.Beta!);
                if (dto.Label != "b" || dto.Alpha is not { Label: "a2" })
                    Interlocked.Increment(ref failures);
            }
        });

        failures.Should().Be(0);
    }

    [Fact]
    public void Map_ConcurrentFirstUseOfManyPairs_AllCorrect()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<LcSource, LcDest>();
            cfg.CreateMap<LcOther, LcOtherDest>();
            cfg.CreateMap<LcNode, LcNodeDto>();
            cfg.CreateMap<LcAlpha, LcAlphaDto>();
            cfg.CreateMap<LcBeta, LcBetaDto>();
        }).CreateMapper();
        var failures = 0;

        Parallel.For(0, 500, i =>
        {
            var ok = (i % 5) switch
            {
                0 => mapper.Map<LcSource, LcDest>(new LcSource { Id = i }).Id == i,
                1 => mapper.Map<LcOther, LcOtherDest>(new LcOther { Code = "x" + i }).Code == "x" + i,
                2 => mapper.Map<LcNode, LcNodeDto>(new LcNode { Name = "n" }).Name == "n",
                3 => mapper.Map<LcAlpha, LcAlphaDto>(new LcAlpha { Label = "a" }).Label == "a",
                _ => mapper.Map<LcBetaDto>(new LcBeta { Label = "b" }).Label == "b",
            };
            if (!ok) Interlocked.Increment(ref failures);
        });

        failures.Should().Be(0);
    }

    [Fact]
    public void CompileMappings_ConcurrentWithFirstUseMaps_AllCorrect()
    {
        var config = TwoMapConfiguration();
        var mapper = config.CreateMapper();
        var failures = 0;

        Parallel.Invoke(
            config.CompileMappings,
            () =>
            {
                for (var i = 0; i < 200; i++)
                {
                    if (mapper.Map<LcSource, LcDest>(new LcSource { Id = i }).Id != i)
                        Interlocked.Increment(ref failures);
                }
            },
            () =>
            {
                for (var i = 0; i < 200; i++)
                {
                    if (mapper.Map<LcOtherDest>(new LcOther { Code = "c" }).Code != "c")
                        Interlocked.Increment(ref failures);
                }
            });

        failures.Should().Be(0);
    }

    [Fact]
    public void Map_BrokenMap_FailsOnFirstMapNotInConstructor_AndCachesTheFailure()
    {
        var boomChecks = 0;
        MapperConfiguration? config = null;
        var construct = () =>
        {
            config = new MapperConfiguration(cfg =>
            {
                cfg.ShouldMapProperty = p =>
                {
                    if (p.Name != nameof(LcBrokenDest.Boom)) return true;
                    Interlocked.Increment(ref boomChecks);
                    throw new InvalidOperationException("boom");
                };
                cfg.CreateMap<LcBroken, LcBrokenDest>();
                cfg.CreateMap<LcSource, LcDest>();
            });
        };
        construct.Should().NotThrow();
        boomChecks.Should().Be(0);
        var mapper = config!.CreateMapper();

        var first = Record.Exception(() => mapper.Map<LcBroken, LcBrokenDest>(new LcBroken { Text = "abc" }));
        var checksAfterFirst = boomChecks;
        var second = Record.Exception(() => mapper.Map<LcBroken, LcBrokenDest>(new LcBroken { Text = "abc" }));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.GetType().Should().Be(first!.GetType());
        checksAfterFirst.Should().BeGreaterThan(0);
        boomChecks.Should().Be(checksAfterFirst);
        mapper.Map<LcSource, LcDest>(new LcSource { Id = 9 }).Id.Should().Be(9);
    }

    [Fact]
    public void CompileMappings_TwoBrokenMaps_ThrowsAggregateException()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.ShouldMapProperty = p => p.Name == nameof(LcBrokenDest.Boom)
                ? throw new InvalidOperationException("boom")
                : true;
            cfg.CreateMap<LcBroken, LcBrokenDest>();
            cfg.CreateMap<LcBroken, LcBrokenDest2>();
            cfg.CreateMap<LcSource, LcDest>();
        });

        var act = () => config.CompileMappings();

        act.Should().Throw<AggregateException>().Which.InnerExceptions.Should().HaveCount(2);
        config.FrozenMaps.Should().ContainKey(Pair<LcSource, LcDest>());
    }

    [Fact]
    public void Map_ParentWithBrokenChildMap_FailsOnlyWhenChildIsMapped()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.ShouldMapProperty = p => p.Name == nameof(LcBrokenDest.Boom)
                ? throw new InvalidOperationException("boom")
                : true;
            cfg.CreateMap<LcBroken, LcBrokenDest>();
            cfg.CreateMap<LcParent, LcParentDto>();
        }).CreateMapper();

        mapper.Map<LcParent, LcParentDto>(new LcParent { Name = "no child" }).Name.Should().Be("no child");
        var withChild = () => mapper.Map<LcParent, LcParentDto>(new LcParent { Name = "p", Child = new LcBroken() });

        withChild.Should().Throw<Exception>();
    }

    [Fact]
    public void CompileMappings_BrokenMap_Throws()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.ShouldMapProperty = p => p.Name == nameof(LcBrokenDest.Boom)
                ? throw new InvalidOperationException("boom")
                : true;
            cfg.CreateMap<LcBroken, LcBrokenDest>();
            cfg.CreateMap<LcSource, LcDest>();
        });

        var act = () => config.CompileMappings();

        act.Should().Throw<Exception>();
        config.FrozenMaps.Should().ContainKey(Pair<LcSource, LcDest>());
    }

    [Fact]
    public void Map_OpenGenericWithRegisteredChild_FirstUse_MapsChild()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(LcWrapper<>), typeof(LcWrapperDto<>));
            cfg.CreateMap<LcSource, LcDest>();
        }).CreateMapper();

        var result = mapper.Map<LcWrapper<LcSource>, LcWrapperDto<LcDest>>(
            new LcWrapper<LcSource> { Value = new LcSource { Id = 3, Name = "three" } });

        result.Value.Id.Should().Be(3);
        result.Value.Name.Should().Be("three");
    }

    [Fact]
    public void Map_SameTypeWithoutCreateMap_FirstUse_CreatesCopy()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<LcSource, LcDest>()).CreateMapper();
        var source = new LcHolder { Item = new LcSource { Id = 6, Name = "six" } };

        var result = mapper.Map<LcHolder, LcHolder>(source);

        result.Should().NotBeSameAs(source);
        result.Item.Id.Should().Be(6);
    }

    [Fact]
    public void Map_IncludeBaseDerivedCompiledFirst_AppliesBaseMemberConfiguration()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<LcBase, LcBaseDto>().ForMember(d => d.Title, o => o.MapFrom(s => "T:" + s.Title));
            cfg.CreateMap<LcDerived, LcDerivedDto>().IncludeBase<LcBase, LcBaseDto>();
        }).CreateMapper();

        var derived = mapper.Map<LcDerived, LcDerivedDto>(new LcDerived { Title = "x", Size = 2 });
        var viaProxy = mapper.Map<LcDerivedDto>(new LcDerivedProxy { Title = "y", Size = 3 });

        derived.Title.Should().Be("T:x");
        derived.Size.Should().Be(2);
        viaProxy.Title.Should().Be("T:y");
        viaProxy.Size.Should().Be(3);
    }
}

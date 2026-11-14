using EggMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class DispatchCacheTests
{
    public class SameTypeItem
    {
        public string Name { get; set; } = "";
        public int Value { get; set; }
    }

    public class DcSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DcOtherSource
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class DcDerivedSource : DcSource
    {
        public string Extra { get; set; } = "";
    }

    public class DcDest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public interface IDcNamed
    {
        string Name { get; }
    }

    public class DcNamed : IDcNamed
    {
        public string Name { get; set; } = "";
    }

    public class DcNamedDest
    {
        public string Name { get; set; } = "";
    }

    public struct DcPointSource
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct DcPointDest
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class DcThrowingSource
    {
        public int Id { get; set; }
    }

    public class DcScopeTag
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    public class DcScopedDest
    {
        public int Id { get; set; }
        public string? Scope { get; set; }
    }

    public class DcScopeResolver : IMemberValueResolver<object, object, int, string?>
    {
        private readonly DcScopeTag _tag;
        public DcScopeResolver(DcScopeTag tag) { _tag = tag; }
        public string? Resolve(object source, object destination, int sourceMember, string? destMember, ResolutionContext context)
            => _tag.Id.ToString();
    }

    private static IMapper FlatMapper() =>
        new MapperConfiguration(cfg => cfg.CreateMap<DcSource, DcDest>()).CreateMapper();

    [Fact]
    public void MapIntoExisting_SameTypeWithoutCreateMap_CalledFirst_MapsIntoInstance()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
        var destination = new SameTypeItem();

        var result = mapper.Map(new SameTypeItem { Name = "a", Value = 1 }, destination);

        result.Should().BeSameAs(destination);
        destination.Name.Should().Be("a");
        destination.Value.Should().Be(1);
    }

    [Fact]
    public void MapIntoExisting_SameTypeWithoutCreateMap_AfterMapNew_MapsIntoInstance()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
        mapper.Map<SameTypeItem, SameTypeItem>(new SameTypeItem { Name = "warm" });
        var destination = new SameTypeItem();

        var result = mapper.Map(new SameTypeItem { Name = "b", Value = 2 }, destination);

        result.Should().BeSameAs(destination);
        destination.Name.Should().Be("b");
        destination.Value.Should().Be(2);
    }

    [Fact]
    public void MapIntoExisting_RepeatedCalls_MapIntoGivenInstance()
    {
        var mapper = FlatMapper();
        var first = new DcDest();
        var second = new DcDest();

        mapper.Map(new DcSource { Id = 1, Name = "one" }, first).Should().BeSameAs(first);
        mapper.Map(new DcSource { Id = 2, Name = "two" }, second).Should().BeSameAs(second);

        first.Id.Should().Be(1);
        first.Name.Should().Be("one");
        second.Id.Should().Be(2);
        second.Name.Should().Be("two");
    }

    [Fact]
    public void MapIntoExisting_NullDestination_CreatesNew()
    {
        var mapper = FlatMapper();
        mapper.Map(new DcSource { Id = 1 }, new DcDest());

        var result = mapper.Map(new DcSource { Id = 7, Name = "n" }, (DcDest)null!);

        result.Should().NotBeNull();
        result.Id.Should().Be(7);
        result.Name.Should().Be("n");
    }

    [Fact]
    public void MapIntoExisting_AfterMapNewWarmedCache_MapsIntoInstance()
    {
        var mapper = FlatMapper();
        mapper.Map<DcSource, DcDest>(new DcSource { Id = 1 });
        var destination = new DcDest();

        var result = mapper.Map(new DcSource { Id = 3, Name = "three" }, destination);

        result.Should().BeSameAs(destination);
        destination.Id.Should().Be(3);
    }

    [Fact]
    public void MapIntoExisting_TwoConfigurations_EachUsesOwnMap()
    {
        var plain = FlatMapper();
        var renamed = new MapperConfiguration(cfg => cfg.CreateMap<DcSource, DcDest>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name + "!"))).CreateMapper();

        for (var i = 0; i < 3; i++)
        {
            plain.Map(new DcSource { Name = "x" }, new DcDest()).Name.Should().Be("x");
            renamed.Map(new DcSource { Name = "x" }, new DcDest()).Name.Should().Be("x!");
        }
    }

    [Fact]
    public void MapIntoExisting_CtxAwareMap_RunsAfterMapOnExistingDestination()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DcSource, DcDest>()
            .AfterMap((s, d) => d.Name = d.Name + "-after")).CreateMapper();

        for (var i = 0; i < 2; i++)
        {
            var destination = new DcDest();
            var result = mapper.Map(new DcSource { Name = "n" }, destination);
            result.Should().BeSameAs(destination);
            destination.Name.Should().Be("n-after");
        }
    }

    [Fact]
    public void MapIntoExisting_ValueTypeDestination_ReturnsMappedValue()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DcPointSource, DcPointDest>()).CreateMapper();

        for (var i = 0; i < 2; i++)
        {
            var result = mapper.Map(new DcPointSource { X = 1 + i, Y = 2 }, new DcPointDest());
            result.X.Should().Be(1 + i);
            result.Y.Should().Be(2);
        }
    }

    [Fact]
    public void MapIntoExisting_ThrowingMember_SameExceptionTypeOnFirstAndSecondCall()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DcThrowingSource, DcDest>()
            .ForMember(d => d.Name, o => o.MapFrom(s => Throw(s)))).CreateMapper();

        var first = Record.Exception(() => mapper.Map(new DcThrowingSource(), new DcDest()));
        var second = Record.Exception(() => mapper.Map(new DcThrowingSource(), new DcDest()));

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        second!.GetType().Should().Be(first!.GetType());
    }

    private static string Throw(DcThrowingSource source) => throw new InvalidOperationException("boom");

    [Fact]
    public void MapObjectSource_Null_ReturnsDefault()
    {
        FlatMapper().Map<DcDest>(null).Should().BeNull();
    }

    [Fact]
    public void MapObjectSource_RepeatedCalls_ReturnsMappedValues()
    {
        var mapper = FlatMapper();

        for (var i = 0; i < 3; i++)
        {
            var result = mapper.Map<DcDest>(new DcSource { Id = i, Name = "n" + i });
            result.Id.Should().Be(i);
            result.Name.Should().Be("n" + i);
        }
    }

    [Fact]
    public void MapObjectSource_AlternatingSourceTypes_MapsEachCorrectly()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DcSource, DcDest>();
            cfg.CreateMap<DcOtherSource, DcDest>().ForMember(d => d.Name, o => o.MapFrom(s => "other:" + s.Name));
        }).CreateMapper();

        for (var i = 0; i < 3; i++)
        {
            mapper.Map<DcDest>(new DcSource { Name = "a" }).Name.Should().Be("a");
            mapper.Map<DcDest>(new DcOtherSource { Name = "b" }).Name.Should().Be("other:b");
        }
    }

    [Fact]
    public void MapObjectSource_DerivedRuntimeType_UsesBaseMap()
    {
        var mapper = FlatMapper();

        for (var i = 0; i < 3; i++)
        {
            var result = mapper.Map<DcDest>(new DcDerivedSource { Id = i, Name = "derived" });
            result.Id.Should().Be(i);
            result.Name.Should().Be("derived");
        }
    }

    [Fact]
    public void MapObjectSource_InterfaceMap_UsesInterfaceMap()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<IDcNamed, DcNamedDest>()).CreateMapper();

        for (var i = 0; i < 2; i++)
            mapper.Map<DcNamedDest>(new DcNamed { Name = "i" + i }).Name.Should().Be("i" + i);
    }

    [Fact]
    public void MapObjectSource_TwoConfigurations_EachUsesOwnMap()
    {
        var plain = FlatMapper();
        var renamed = new MapperConfiguration(cfg => cfg.CreateMap<DcSource, DcDest>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name + "?"))).CreateMapper();

        for (var i = 0; i < 3; i++)
        {
            plain.Map<DcDest>(new DcSource { Name = "x" }).Name.Should().Be("x");
            renamed.Map<DcDest>(new DcSource { Name = "x" }).Name.Should().Be("x?");
        }
    }

    [Fact]
    public void MapObjectSource_BoxedValueTypeSource_Maps()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DcPointSource, DcPointDest>()).CreateMapper();

        for (var i = 0; i < 2; i++)
        {
            object boxed = new DcPointSource { X = 5, Y = i };
            var result = mapper.Map<DcPointDest>(boxed);
            result.X.Should().Be(5);
            result.Y.Should().Be(i);
        }
    }

    [Fact]
    public void MapObjectSource_BoxedIntWithNullableMap_UsesNullableMap()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<int?, string>()
            .ConvertUsing(s => s.HasValue ? "v" + s.Value : "none")).CreateMapper();

        for (var i = 0; i < 2; i++)
            mapper.Map<string>(42).Should().Be("v42");
    }

    [Fact]
    public void MapObjectSource_SameTypeWithoutCreateMap_ReturnsCopy()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();
        var source = new SameTypeItem { Name = "copy", Value = 9 };

        for (var i = 0; i < 2; i++)
        {
            var result = mapper.Map<SameTypeItem>(source);
            result.Should().NotBeSameAs(source);
            result.Name.Should().Be("copy");
            result.Value.Should().Be(9);
        }
    }

    [Fact]
    public void MapObjectSource_CtxAwareMap_RunsAfterMap()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DcSource, DcDest>()
            .AfterMap((s, d) => d.Name = "after:" + s.Name)).CreateMapper();

        for (var i = 0; i < 3; i++)
            mapper.Map<DcDest>(new DcSource { Name = "n" }).Name.Should().Be("after:n");
    }

    [Fact]
    public void MapObjectSource_CtxAwareMap_UsesServiceProviderOfEachMapper()
    {
        var services = new ServiceCollection();
        services.AddScoped<DcScopeTag>();
        services.AddEggMapper(cfg => cfg.CreateMap<DcSource, DcScopedDest>()
            .ForMember(d => d.Scope, o => o.MapFrom<DcScopeResolver, int>(s => s.Id)));
        var provider = services.BuildServiceProvider();

        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();
        var tag1 = scope1.ServiceProvider.GetRequiredService<DcScopeTag>().Id.ToString();
        var tag2 = scope2.ServiceProvider.GetRequiredService<DcScopeTag>().Id.ToString();
        var mapper1 = scope1.ServiceProvider.GetRequiredService<IMapper>();
        var mapper2 = scope2.ServiceProvider.GetRequiredService<IMapper>();

        for (var i = 0; i < 2; i++)
        {
            mapper1.Map<DcScopedDest>(new DcSource { Id = 1 }).Scope.Should().Be(tag1);
            mapper2.Map<DcScopedDest>(new DcSource { Id = 1 }).Scope.Should().Be(tag2);
        }
    }

    [Fact]
    public void MapObjectSource_WithOptions_RunsCallSiteAfterMapAfterCacheWarm()
    {
        var mapper = FlatMapper();
        mapper.Map<DcDest>(new DcSource { Name = "warm" });

        var result = mapper.Map<DcDest>(new DcSource { Name = "n" }, opts => opts.AfterMap((s, d) => d.Name += "+opts"));

        result.Name.Should().Be("n+opts");
    }

    [Fact]
    public void CtxFreeBoxedMaps_NullContext_MapWithoutContext()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<DcSource, DcDest>();
            cfg.CreateMap<DcPointSource, DcPointDest>();
        });
        config.CompileMappings();

        config.FrozenCtxFreeMaps.Keys.Should().NotBeEmpty();
        foreach (var key in config.FrozenCtxFreeMaps.Keys)
        {
            var source = Activator.CreateInstance(key.SourceType)!;
            var act = () => config.FrozenMaps[key](source, null, null!);
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void Dispatch_ParallelCallsAcrossTwoConfigurations_EachUsesOwnMap()
    {
        var plain = FlatMapper();
        var renamed = new MapperConfiguration(cfg => cfg.CreateMap<DcSource, DcDest>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name + "#"))).CreateMapper();
        var failures = 0;

        Parallel.For(0, 2000, i =>
        {
            var useRenamed = i % 2 == 0;
            var mapper = useRenamed ? renamed : plain;
            var expected = useRenamed ? "x#" : "x";
            var viaObject = mapper.Map<DcDest>(new DcSource { Name = "x" }).Name;
            var viaInto = mapper.Map(new DcSource { Name = "x" }, new DcDest()).Name;
            if (viaObject != expected || viaInto != expected)
                Interlocked.Increment(ref failures);
        });

        failures.Should().Be(0);
    }
}

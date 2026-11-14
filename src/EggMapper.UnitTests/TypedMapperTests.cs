using EggMapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class TypedMapperTests
{
    public class TmSource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class TmDest
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Scope { get; set; }
    }

    public class TmUnmapped
    {
        public int Id { get; set; }
    }

    public class TmScopeTag
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }

    public class TmScopeResolver : IMemberValueResolver<object, object, int, string?>
    {
        private readonly TmScopeTag _tag;
        public TmScopeResolver(TmScopeTag tag) { _tag = tag; }
        public string? Resolve(object source, object destination, int sourceMember, string? destMember, ResolutionContext context)
            => _tag.Id;
    }

    private static MapperConfiguration Config() => new(cfg => cfg.CreateMap<TmSource, TmDest>());

    [Fact]
    public void Map_ConfiguredPair_MapsLikeIMapper()
    {
        var config = Config();
        var typed = config.CreateMapper<TmSource, TmDest>();
        var untyped = config.CreateMapper();

        for (var i = 0; i < 3; i++)
        {
            var source = new TmSource { Id = i, Name = "n" + i };
            typed.Map(source).Should().BeEquivalentTo(untyped.Map<TmSource, TmDest>(source));
        }
    }

    [Fact]
    public void Map_IntoExisting_MapsIntoGivenInstance()
    {
        var typed = Config().CreateMapper<TmSource, TmDest>();

        for (var i = 0; i < 3; i++)
        {
            var destination = new TmDest();
            typed.Map(new TmSource { Id = i, Name = "x" }, destination).Should().BeSameAs(destination);
            destination.Id.Should().Be(i);
        }
    }

    [Fact]
    public void MapList_Items_MapsAll()
    {
        var typed = Config().CreateMapper<TmSource, TmDest>();

        typed.MapList(new[] { new TmSource { Id = 1 }, new TmSource { Id = 2 } }).Select(d => d.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Patch_NullMembers_KeepsDestinationValues()
    {
        var typed = Config().CreateMapper<TmSource, TmDest>();
        var destination = new TmDest { Id = 1, Name = "keep" };

        typed.Patch(new TmSource { Id = 2, Name = null }, destination);

        destination.Id.Should().Be(2);
        destination.Name.Should().Be("keep");
    }

    [Fact]
    public void Map_NullSource_ReturnsDefault()
    {
        var typed = Config().CreateMapper<TmSource, TmDest>();
        typed.Map(new TmSource());

        typed.Map(null!).Should().BeNull();
    }

    [Fact]
    public void Map_NullableValueTypeConvertUsingWithNullSource_UsesConverter()
    {
        var typed = new MapperConfiguration(cfg => cfg.CreateMap<int?, string>()
            .ConvertUsing(s => s.HasValue ? "v" + s.Value : "none")).CreateMapper<int?, string>();

        typed.Map(7).Should().Be("v7");
        typed.Map(null).Should().Be("none");
    }

    [Fact]
    public void Map_UnconfiguredPair_ThrowsSameAsIMapper()
    {
        var config = Config();
        var typed = config.CreateMapper<TmUnmapped, TmDest>();

        var typedError = Record.Exception(() => typed.Map(new TmUnmapped()));
        var untypedError = Record.Exception(() => config.CreateMapper().Map<TmUnmapped, TmDest>(new TmUnmapped()));

        typedError.Should().NotBeNull();
        typedError!.GetType().Should().Be(untypedError!.GetType());
        typedError.Message.Should().Contain("No mapping configured");
    }

    [Fact]
    public void DI_UnconfiguredPair_ResolvesAndThrowsOnFirstMap()
    {
        var services = new ServiceCollection();
        services.AddEggMapper(cfg => cfg.CreateMap<TmSource, TmDest>());
        using var provider = services.BuildServiceProvider();

        var typed = provider.GetRequiredService<IMapper<TmUnmapped, TmDest>>();

        var act = () => typed.Map(new TmUnmapped());
        act.Should().Throw<InvalidOperationException>().WithMessage("*No mapping configured*");
    }

    [Fact]
    public void DI_ResolveTypedMapper_UsesScopedServiceProviderForResolvers()
    {
        var services = new ServiceCollection();
        services.AddScoped<TmScopeTag>();
        services.AddEggMapper(cfg => cfg.CreateMap<TmSource, TmDest>()
            .ForMember(d => d.Scope, o => o.MapFrom<TmScopeResolver, int>(s => s.Id)));
        using var provider = services.BuildServiceProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var typed1 = scope1.ServiceProvider.GetRequiredService<IMapper<TmSource, TmDest>>();
        var typed2 = scope2.ServiceProvider.GetRequiredService<IMapper<TmSource, TmDest>>();

        for (var i = 0; i < 2; i++)
        {
            typed1.Map(new TmSource { Id = 1 }).Scope.Should().Be(scope1.ServiceProvider.GetRequiredService<TmScopeTag>().Id);
            typed2.Map(new TmSource { Id = 1 }).Scope.Should().Be(scope2.ServiceProvider.GetRequiredService<TmScopeTag>().Id);
        }
    }

    [Fact]
    public void DI_ResolveFromScopeCreatedInsideSingleton_UsesThatScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<TmScopeTag>();
        services.AddEggMapper(cfg => cfg.CreateMap<TmSource, TmDest>()
            .ForMember(d => d.Scope, o => o.MapFrom<TmScopeResolver, int>(s => s.Id)));
        services.AddSingleton<TmSingletonWorker>();
        using var provider = services.BuildServiceProvider();

        var (mapped, expected) = provider.GetRequiredService<TmSingletonWorker>().MapInNewScope();

        mapped.Should().Be(expected);
    }

    public class TmSingletonWorker
    {
        private readonly IServiceScopeFactory _scopes;
        public TmSingletonWorker(IServiceScopeFactory scopes) { _scopes = scopes; }

        public (string? Mapped, string Expected) MapInNewScope()
        {
            using var scope = _scopes.CreateScope();
            var typed = scope.ServiceProvider.GetRequiredService<IMapper<TmSource, TmDest>>();
            return (typed.Map(new TmSource { Id = 1 }).Scope, scope.ServiceProvider.GetRequiredService<TmScopeTag>().Id);
        }
    }

    [Fact]
    public void Map_WrappingCustomIMapper_ForwardsToIt()
    {
        var custom = new RecordingMapper();
        var typed = new TypedMapper<TmSource, TmDest>(custom);

        typed.Map(new TmSource { Id = 5 });
        typed.Map(new TmSource { Id = 6 });

        custom.Calls.Should().Be(2);
    }

    private sealed class RecordingMapper : IMapper
    {
        public int Calls;
        public TDestination Map<TDestination>(object? source) => throw new NotSupportedException();
        public TDestination Map<TSource, TDestination>(TSource source) { Calls++; return default!; }
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination) { Calls++; return destination; }
        public TDestination Map<TDestination>(object? source, Action<IMappingOperationOptions<object, TDestination>> opts) => throw new NotSupportedException();
        public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts) => throw new NotSupportedException();
        public object Map(object source, Type sourceType, Type destinationType) => throw new NotSupportedException();
        public List<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source) => throw new NotSupportedException();
        public TDestination Patch<TSource, TDestination>(TSource source, TDestination destination) => throw new NotSupportedException();
    }
}

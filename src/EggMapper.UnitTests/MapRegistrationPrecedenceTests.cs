using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class MapRegistrationPrecedenceTests
{
    public class Entity
    {
        public string Name { get; set; } = "";
        public string Secret { get; set; } = "";
    }

    public class EntityDto { public string Name { get; set; } = ""; }

    public class EntityToDtoWithReverseProfile : Profile
    {
        public EntityToDtoWithReverseProfile() => CreateMap<Entity, EntityDto>().ReverseMap();
    }

    public class DtoToEntityProfile : Profile
    {
        public DtoToEntityProfile() =>
            CreateMap<EntityDto, Entity>()
                .ForMember(d => d.Name, o => o.MapFrom(s => "explicit:" + s.Name))
                .ForMember(d => d.Secret, o => o.Ignore());
    }

    public class EntityProfile : Profile
    {
        public EntityProfile() => CreateMap<Entity, EntityDto>().ForMember(d => d.Name, o => o.MapFrom(s => "first:" + s.Name));
    }

    public class LegacyEntityProfile : Profile
    {
        public LegacyEntityProfile() => CreateMap<Entity, EntityDto>().ForMember(d => d.Name, o => o.MapFrom(s => "second:" + s.Name));
    }

    private static string MapToEntityName(Action<IMapperConfigurationExpression> configure) =>
        new MapperConfiguration(configure).CreateMapper().Map<EntityDto, Entity>(new EntityDto { Name = "x" }).Name;

    [Fact]
    public void AddProfile_ReverseMapThenExplicitMapInLaterProfile_UsesTheExplicitMap()
    {
        var name = MapToEntityName(cfg =>
        {
            cfg.AddProfile<EntityToDtoWithReverseProfile>();
            cfg.AddProfile<DtoToEntityProfile>();
        });

        name.Should().Be("explicit:x");
    }

    [Fact]
    public void AddProfile_ExplicitMapThenReverseMapInLaterProfile_KeepsTheExplicitMap()
    {
        var name = MapToEntityName(cfg =>
        {
            cfg.AddProfile<DtoToEntityProfile>();
            cfg.AddProfile<EntityToDtoWithReverseProfile>();
        });

        name.Should().Be("explicit:x");
    }

    [Fact]
    public void AddProfile_SamePairInTwoProfiles_UsesTheFirstAndValidationNamesBoth()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityProfile>();
            cfg.AddProfile<LegacyEntityProfile>();
        });

        config.CreateMapper().Map<Entity, EntityDto>(new Entity { Name = "x" }).Name.Should().Be("first:x");
        var act = () => config.AssertConfigurationIsValid();
        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().StartWith("EggMapper configuration is invalid: 1 map configured in more than one place.")
           .And.Contain("CreateMap<Entity, EntityDto>() is configured in EntityProfile and LegacyEntityProfile; only the map in EntityProfile is used.");
    }

    [Fact]
    public void CreateMap_RootMapAfterProfileMapForSamePair_UsesTheRootMapAndValidationNamesBoth()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityProfile>();
            cfg.CreateMap<Entity, EntityDto>();
        });

        config.CreateMapper().Map<Entity, EntityDto>(new Entity { Name = "x" }).Name.Should().Be("x");
        var act = () => config.AssertConfigurationIsValid();
        act.Should().Throw<InvalidOperationException>()
           .Which.Message.Should().Contain("CreateMap<Entity, EntityDto>() is configured in the root configuration and EntityProfile; only the map in the root configuration is used.");
    }

    [Fact]
    public void AddProfile_SameProfileTwice_IsNotReportedAsDuplicate()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<EntityProfile>();
            cfg.AddProfile<EntityProfile>();
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void CreateMap_RootMapReplacesEarlierRootMap_IsNotReportedAsDuplicate()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Entity, EntityDto>();
            cfg.CreateMap<Entity, EntityDto>();
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }
}

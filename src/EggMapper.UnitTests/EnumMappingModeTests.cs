using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class EnumMappingModeTests
{
    public enum EmSource { Pending = 0, Active = 1, Closed = 2, Archived = 3 }
    public enum EmDest { Active = 0, Closed = 1, Pending = 2, Deleted = 9 }
    public enum EmUpperDest { PENDING = 5, ACTIVE = 6, CLOSED = 7, ARCHIVED = 8 }
    public enum EmByteSource : byte { Low = 1, High = 200 }
    public enum EmLongDest : long { High = 5_000_000_000, Low = 7 }
    public enum EmULongSource : ulong { Big = ulong.MaxValue - 1, Small = 2 }
    public enum EmULongDest : ulong { Small = 20, Big = 30 }
    public enum EmAliasedSource { None = 0, Default = 0, One = 1 }
    public enum EmAliasedDest { One = 10, None = 20 }

    [Flags]
    public enum EmFlagsSource { None = 0, Read = 1, Write = 2, Execute = 4 }

    [Flags]
    public enum EmFlagsDest { None = 0, Execute = 1, Read = 8, Write = 16 }

    [Flags]
    public enum EmFlagsPartialDest { None = 0, Read = 8 }

    public class EmHolder
    {
        public EmSource Status { get; set; }
        public EmSource? Maybe { get; set; }
        public List<EmSource> History { get; set; } = new();
    }

    public class EmHolderDto
    {
        public EmDest Status { get; set; }
        public EmDest? Maybe { get; set; }
        public List<EmDest> History { get; set; } = new();
    }

    public class EmNullableToPlain
    {
        public EmSource? Status { get; set; }
    }

    public class EmPlainDto
    {
        public EmDest Status { get; set; }
    }

    public record EmRecordDto(EmDest Status);

    public class EmRecordSource
    {
        public EmSource Status { get; set; }
    }

    public enum EmSignedSource : sbyte { Negative = -5, Zero = 0 }
    public enum EmSignedDest : sbyte { Zero = 1, Negative = 2 }

    public class EmInner
    {
        public EmSource Status { get; set; }
    }

    public class EmOuter
    {
        public EmInner Inner { get; set; } = new();
    }

    public class EmFlatDto
    {
        public EmDest InnerStatus { get; set; }
    }

    public class EmMaybeDto
    {
        public EmDest? Status { get; set; }
    }

    public class EmDictionaryHolder
    {
        public Dictionary<string, EmSource> Values { get; set; } = new();
    }

    public class EmDictionaryHolderDto
    {
        public Dictionary<string, EmDest> Values { get; set; } = new();
    }

    private static IMapper Mapper(Action<IMapperConfigurationExpression> configure) =>
        new MapperConfiguration(configure).CreateMapper();

    [Fact]
    public void Map_DifferentEnumsDefault_MapsByNameLikeAutoMapper()
    {
        var mapper = Mapper(cfg => cfg.CreateMap<EmHolder, EmHolderDto>());

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Pending }).Status.Should().Be(EmDest.Pending);
    }

    [Fact]
    public void Map_GlobalByValue_MapsByValue()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByValue;
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Pending }).Status.Should().Be(EmDest.Active);
    }

    [Fact]
    public void Map_RecordConstructorArgumentOnFlexiblePath_UsesDefaultByName()
    {
        var mapper = Mapper(cfg => cfg.CreateMap<EmRecordSource, EmRecordDto>().AfterMap((s, d) => { }));

        mapper.Map<EmRecordSource, EmRecordDto>(new EmRecordSource { Status = EmSource.Closed }).Status.Should().Be(EmDest.Closed);
    }

    [Fact]
    public void Map_GlobalByName_MapsMatchingNames()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        var result = mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Pending, Maybe = EmSource.Closed });

        result.Status.Should().Be(EmDest.Pending);
        result.Maybe.Should().Be(EmDest.Closed);
    }

    [Fact]
    public void Map_GlobalByName_MissingName_FallsBackToValue()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Archived }).Status.Should().Be((EmDest)3);
    }

    [Fact]
    public void Map_ByName_MatchesIgnoringCase()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<EmSource, EmUpperDest>(EmSource.Active).Should().Be(EmUpperDest.ACTIVE);
    }

    [Fact]
    public void Map_ConvertUsingEnumMappingByName_OverridesGlobalByValue()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.CreateMap<EmSource, EmDest>().ConvertUsingEnumMapping(o => o.MapByName());
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Closed }).Status.Should().Be(EmDest.Closed);
        mapper.Map<EmSource, EmDest>(EmSource.Pending).Should().Be(EmDest.Pending);
    }

    [Fact]
    public void Map_MapValue_OverridesNameRule()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.CreateMap<EmSource, EmDest>().ConvertUsingEnumMapping(o => o.MapByName().MapValue(EmSource.Archived, EmDest.Deleted));
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Archived }).Status.Should().Be(EmDest.Deleted);
        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Active }).Status.Should().Be(EmDest.Active);
    }

    [Fact]
    public void Map_ConvertUsingOnEnumPair_IsUsedForMembers()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.CreateMap<EmSource, EmDest>().ConvertUsing(s => EmDest.Deleted);
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Active }).Status.Should().Be(EmDest.Deleted);
    }

    [Fact]
    public void Map_GlobalTypeConverter_BeatsModeButLosesToConvertUsingEnumMapping()
    {
        var withConverter = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.AddTypeConverter<EmSource, EmDest>(s => EmDest.Deleted);
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });
        var withPairOptions = Mapper(cfg =>
        {
            cfg.AddTypeConverter<EmSource, EmDest>(s => EmDest.Deleted);
            cfg.CreateMap<EmSource, EmDest>().ConvertUsingEnumMapping(o => o.MapByName());
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        withConverter.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Active }).Status.Should().Be(EmDest.Deleted);
        withPairOptions.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Active }).Status.Should().Be(EmDest.Active);
    }

    [Fact]
    public void Map_NullableSourceNull_ToNonNullable_GivesDefault()
    {
        var mapper = Mapper(cfg => cfg.CreateMap<EmNullableToPlain, EmPlainDto>());

        mapper.Map<EmNullableToPlain, EmPlainDto>(new EmNullableToPlain()).Status.Should().Be(default(EmDest));
    }

    [Fact]
    public void Map_NullableBothSides_KeepsNull()
    {
        var mapper = Mapper(cfg => cfg.CreateMap<EmHolder, EmHolderDto>());

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder()).Maybe.Should().BeNull();
    }

    [Fact]
    public void Map_DifferentUnderlyingTypes_ByName()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<EmByteSource, EmLongDest>(EmByteSource.High).Should().Be(EmLongDest.High);
        mapper.Map<EmULongSource, EmULongDest>(EmULongSource.Big).Should().Be(EmULongDest.Big);
    }

    [Fact]
    public void Map_AliasedSourceValues_ByName_UsesCanonicalName()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<EmAliasedSource, EmAliasedDest>(EmAliasedSource.One).Should().Be(EmAliasedDest.One);
        var aliased = mapper.Map<EmAliasedSource, EmAliasedDest>(EmAliasedSource.Default);
        aliased.Should().Be(EmAliasedDest.None);
    }

    [Fact]
    public void Map_FlagsByName_MapsEachFlag()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<EmFlagsSource, EmFlagsDest>(EmFlagsSource.Read | EmFlagsSource.Execute)
            .Should().Be(EmFlagsDest.Read | EmFlagsDest.Execute);
        mapper.Map<EmFlagsSource, EmFlagsDest>(EmFlagsSource.None).Should().Be(EmFlagsDest.None);
    }

    [Fact]
    public void Map_FlagsByName_PartialNames_FallsBackToValue()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<EmFlagsSource, EmFlagsPartialDest>(EmFlagsSource.Write).Should().Be((EmFlagsPartialDest)2);
    }

    [Fact]
    public void Map_TopLevelEnumPairWithoutCreateMap_Maps()
    {
        var byValue = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByValue);
        var byName = Mapper(cfg => { });

        byValue.Map<EmSource, EmDest>(EmSource.Closed).Should().Be(EmDest.Pending);
        byName.Map<EmSource, EmDest>(EmSource.Closed).Should().Be(EmDest.Closed);
        byName.Map<EmDest>(EmSource.Active).Should().Be(EmDest.Active);
    }

    [Fact]
    public void Map_CreateMapOnEnumPairWithoutOptions_UsesGlobalMode()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmSource, EmDest>();
        });

        mapper.Map<EmSource, EmDest>(EmSource.Pending).Should().Be(EmDest.Pending);
    }

    [Fact]
    public void Map_FlexiblePathWithEnumMember_MapsInsteadOfThrowing()
    {
        var afterMapRan = false;
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmHolder, EmHolderDto>().AfterMap((s, d) => afterMapRan = true);
        });

        var result = mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Status = EmSource.Closed, History = { EmSource.Active } });

        result.Status.Should().Be(EmDest.Closed);
        result.History.Should().Equal(EmDest.Active);
        afterMapRan.Should().BeTrue();
    }

    [Fact]
    public void Map_ListOfEnums_ByName()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { History = { EmSource.Pending, EmSource.Closed } })
            .History.Should().Equal(EmDest.Pending, EmDest.Closed);
    }

    [Fact]
    public void Map_RecordConstructorArgument_ByName()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmRecordSource, EmRecordDto>();
        });

        mapper.Map<EmRecordSource, EmRecordDto>(new EmRecordSource { Status = EmSource.Active }).Status.Should().Be(EmDest.Active);
    }

    [Fact]
    public void Map_ListMemberWithPerPairCreateMap_MapsEveryElementByName()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.CreateMap<EmSource, EmDest>().ConvertUsingEnumMapping(o => o.MapByName());
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        mapper.Map<EmHolder, EmHolderDto>(new EmHolder { History = { EmSource.Pending, EmSource.Closed } })
            .History.Should().Equal(EmDest.Pending, EmDest.Closed);
    }

    [Fact]
    public void MapList_EnumPairWithPerPairOptions_MapsEachValue()
    {
        var mapper = Mapper(cfg => cfg.CreateMap<EmSource, EmDest>().ConvertUsingEnumMapping(o => o.MapByName()));

        mapper.MapList<EmSource, EmDest>(new[] { EmSource.Pending, EmSource.Active }).Should().Equal(EmDest.Pending, EmDest.Active);
    }

    [Fact]
    public void Map_FlattenedEnumMember_UsesByName()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmOuter, EmFlatDto>();
        });

        mapper.Map<EmOuter, EmFlatDto>(new EmOuter { Inner = { Status = EmSource.Closed } }).InnerStatus.Should().Be(EmDest.Closed);
    }

    [Fact]
    public void Patch_EnumMember_UsesByName()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });
        var destination = new EmHolderDto();

        mapper.Patch(new EmHolder { Status = EmSource.Closed }, destination);

        destination.Status.Should().Be(EmDest.Closed);
    }

    [Fact]
    public void Map_GlobalTypeConverter_AppliesToNullableListDictionaryAndTopLevel()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.EnumMapping = EnumMappingMode.ByName;
            cfg.AddTypeConverter<EmSource, EmDest>(s => EmDest.Deleted);
            cfg.CreateMap<EmHolder, EmHolderDto>();
            cfg.CreateMap<EmDictionaryHolder, EmDictionaryHolderDto>();
        });

        var holder = mapper.Map<EmHolder, EmHolderDto>(new EmHolder { Maybe = EmSource.Active, History = { EmSource.Active } });
        holder.Maybe.Should().Be(EmDest.Deleted);
        holder.History.Should().Equal(EmDest.Deleted);
        mapper.Map<EmDictionaryHolder, EmDictionaryHolderDto>(new EmDictionaryHolder { Values = { ["a"] = EmSource.Active } })
            .Values["a"].Should().Be(EmDest.Deleted);
        mapper.Map<EmSource, EmDest>(EmSource.Active).Should().Be(EmDest.Deleted);
    }

    [Fact]
    public void Map_ConvertUsingOnPair_NullableDestinationMember_UsesConverter()
    {
        var mapper = Mapper(cfg =>
        {
            cfg.CreateMap<EmSource, EmDest>().ConvertUsing(s => EmDest.Deleted);
            cfg.CreateMap<EmRecordSource, EmMaybeDto>();
        });

        mapper.Map<EmRecordSource, EmMaybeDto>(new EmRecordSource { Status = EmSource.Active }).Status.Should().Be(EmDest.Deleted);
    }

    [Fact]
    public void Map_NegativeSignedUnderlying_ByName()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<EmSignedSource, EmSignedDest>(EmSignedSource.Negative).Should().Be(EmSignedDest.Negative);
    }

    [Fact]
    public void Map_MapValueForUndefinedSourceValue_UsesOverride()
    {
        var mapper = Mapper(cfg => cfg.CreateMap<EmSource, EmDest>()
            .ConvertUsingEnumMapping(o => o.MapValue((EmSource)99, EmDest.Deleted)));

        mapper.Map<EmSource, EmDest>((EmSource)99).Should().Be(EmDest.Deleted);
    }

    [Fact]
    public void ProjectTo_EnumPairWithCreateMap_MapsByValue()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EmSource, EmDest>().ConvertUsingEnumMapping(o => o.MapByName());
            cfg.CreateMap<EmHolder, EmHolderDto>();
        });

        var projection = config.BuildProjection<EmHolder, EmHolderDto>().Compile();

        projection(new EmHolder { Status = EmSource.Closed }).Status.Should().Be((EmDest)2);
    }

    [Fact]
    public void Map_ListOfEnumsToListWithoutCreateMap_Maps()
    {
        var mapper = Mapper(cfg => cfg.EnumMapping = EnumMappingMode.ByName);

        mapper.Map<List<EmDest>>(new List<EmSource> { EmSource.Closed, EmSource.Pending }).Should().Equal(EmDest.Closed, EmDest.Pending);
    }

    [Fact]
    public void ProjectTo_EnumMember_MapsByValue()
    {
        var config = new MapperConfiguration(cfg => cfg.CreateMap<EmHolder, EmHolderDto>());

        var projection = config.BuildProjection<EmHolder, EmHolderDto>().Compile();

        projection(new EmHolder { Status = EmSource.Closed }).Status.Should().Be((EmDest)2);
    }
}

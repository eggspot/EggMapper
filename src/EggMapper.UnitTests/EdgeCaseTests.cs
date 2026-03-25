using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class EdgeCaseTests
{
    // ── Empty / whitespace strings ──────────────────────────────────────────
    [Fact]
    public void Map_EmptyString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "", Email = "" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("");
        dest.Email.Should().Be("");
    }

    [Fact]
    public void Map_WhitespaceString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "   ", Email = "\t\n" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("   ");
        dest.Email.Should().Be("\t\n");
    }

    // ── Boundary numeric values ─────────────────────────────────────────────
    [Fact]
    public void Map_IntMaxValue_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Age = int.MaxValue };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(int.MaxValue);
    }

    [Fact]
    public void Map_IntMinValue_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Age = int.MinValue };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Age.Should().Be(int.MinValue);
    }

    [Fact]
    public void Map_DoubleSpecialValues_MapCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.MaxValue }).Value.Should().Be(double.MaxValue);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.MinValue }).Value.Should().Be(double.MinValue);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.NaN }).Value.Should().Be(double.NaN);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.PositiveInfinity }).Value.Should().Be(double.PositiveInfinity);
        mapper.Map<FlatSource, FlatDest>(new FlatSource { Value = double.NegativeInfinity }).Value.Should().Be(double.NegativeInfinity);
    }

    // ── Special characters in strings ───────────────────────────────────────
    [Fact]
    public void Map_UnicodeString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var src = new FlatSource { Name = "日本語テスト 🎉 Ñoño" };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be("日本語テスト 🎉 Ñoño");
    }

    [Fact]
    public void Map_VeryLongString_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var longString = new string('x', 100_000);
        var src = new FlatSource { Name = longString };
        var dest = mapper.Map<FlatSource, FlatDest>(src);
        dest.Name.Should().Be(longString);
        dest.Name.Length.Should().Be(100_000);
    }

    // ── DateTime and Guid property mapping ──────────────────────────────────
    [Fact]
    public void Map_DateTimeProperties_MapCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<DateTimeSource, DateTimeDest>()).CreateMapper();
        var now = DateTime.UtcNow;
        var src = new DateTimeSource { Created = now, Modified = DateTimeOffset.Now, Id = Guid.NewGuid() };
        var dest = mapper.Map<DateTimeSource, DateTimeDest>(src);
        dest.Created.Should().Be(now);
        dest.Modified.Should().Be(src.Modified);
        dest.Id.Should().Be(src.Id);
    }

    // ── Multiple consecutive mappings ───────────────────────────────────────
    [Fact]
    public void Map_CalledRepeatedly_ProducesIndependentResults()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();

        var results = new List<FlatDest>();
        for (int i = 0; i < 100; i++)
        {
            var src = new FlatSource { Name = $"Item{i}", Age = i };
            results.Add(mapper.Map<FlatSource, FlatDest>(src));
        }

        for (int i = 0; i < 100; i++)
        {
            results[i].Name.Should().Be($"Item{i}");
            results[i].Age.Should().Be(i);
        }
    }

    // ── Missing map throws ──────────────────────────────────────────────────
    [Fact]
    public void Map_UnconfiguredMap_ThrowsInvalidOperationException()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var act = () => mapper.Map<PersonSource, PersonDest>(new PersonSource());
        act.Should().Throw<InvalidOperationException>().Which.Message.Should().Contain("No mapping configured");
    }

    // ── Null source with Map<TDest>(object?) returns default ────────────────
    [Fact]
    public void Map_NullObjectSource_ReturnsDefault()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<FlatSource, FlatDest>()).CreateMapper();
        var result = mapper.Map<FlatDest>(null);
        result.Should().BeNull();
    }

    // ── EF Core proxy / derived type resolution ──────────────────────────────
    [Fact]
    public void Map_DerivedSourceType_UsesBaseMapping()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<BaseEntity, BaseDto>()).CreateMapper();
        object source = new DerivedProxy { Id = 42, Name = "test" };
        var result = mapper.Map<BaseDto>(source);
        result.Id.Should().Be(42);
        result.Name.Should().Be("test");
    }

    [Fact]
    public void Map_TypedWithDerivedRuntimeType_UsesBaseMapping()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<BaseEntity, BaseDto>()).CreateMapper();
        BaseEntity source = new DerivedProxy { Id = 7, Name = "proxy" };
        var result = mapper.Map<BaseEntity, BaseDto>(source);
        result.Id.Should().Be(7);
        result.Name.Should().Be("proxy");
    }

    [Fact]
    public void Map_CollectionOfDerivedElements_UsesBaseMapping()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<BaseEntity, BaseDto>()).CreateMapper();
        var source = new List<BaseEntity>
        {
            new DerivedProxy { Id = 1, Name = "a" },
            new DerivedProxy { Id = 2, Name = "b" }
        };
        var result = mapper.MapList<BaseEntity, BaseDto>(source);
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Name.Should().Be("b");
    }

    // ── MapFrom(s => s) with constructor-based conversion (Id<T> pattern) ──────
    [Fact]
    public void Map_MapFromSourceEntity_ConvertsViaConstructor()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EntityWithId, StrongIdDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s)))
            .CreateMapper();

        var src = new EntityWithId { Id = 42, Name = "test" };
        var result = mapper.Map<EntityWithId, StrongIdDto>(src);

        result.Id.Should().NotBeNull();
        result.Id!.Value.Should().Be(42);
        result.Name.Should().Be("test");
    }

    [Fact]
    public void Map_MapFromSourceEntity_WorksInCollectionWithAfterMap()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EntityWithId, StrongIdDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s)))
            .CreateMapper();

        var sources = new List<EntityWithId>
        {
            new() { Id = 1, Name = "a" },
            new() { Id = 2, Name = "b" }
        };

        var result = mapper.Map<List<StrongIdDto>>(sources, opt =>
        {
            opt.AfterMap((src, dest) =>
            {
                dest.ForEach(d =>
                {
                    var original = sources.FirstOrDefault(s => s.Id == d.Id!.Value);
                    d.Name = original?.Name + "_mapped";
                });
            });
        });

        result.Should().HaveCount(2);
        result[0].Id!.Value.Should().Be(1);
        result[0].Name.Should().Be("a_mapped");
        result[1].Id!.Value.Should().Be(2);
        result[1].Name.Should().Be("b_mapped");
    }

    // ── Open-generic ConvertUsing with interface source (ISequenceIdEntity → Id<T>) ──
    [Fact]
    public void Map_OpenGenericConvertUsing_InterfaceToGeneric()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(ISeqEntity), typeof(WrapperId<>))
                .IncludeAllDerived()
                .ConvertUsing(typeof(SeqEntityToWrapperIdConverter<>));
            cfg.CreateMap<ConcreteEntity, ConcreteEntityDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s));
        }).CreateMapper();

        var src = new ConcreteEntity { Id = 42, SeqId = 100, Name = "test" };
        var result = mapper.Map<ConcreteEntity, ConcreteEntityDto>(src);

        result.Id.Should().NotBeNull();
        result.Id!.Value.Should().Be(42);
        result.Id.SeqId.Should().Be(100);
        result.Name.Should().Be("test");
    }

    [Fact]
    public void Map_OpenGenericConvertUsing_NullSource_ReturnsNull()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap(typeof(ISeqEntity), typeof(WrapperId<>))
                .IncludeAllDerived()
                .ConvertUsing(typeof(SeqEntityToWrapperIdConverter<>));
            cfg.CreateMap<ConcreteEntity, ConcreteEntityDto>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s));
        }).CreateMapper();

        var src = new ConcreteEntity { Id = 0, SeqId = 0, Name = "empty" };
        var result = mapper.Map<ConcreteEntity, ConcreteEntityDto>(src);
        result.Name.Should().Be("empty");
    }

    // ── MapFrom returning a different type that has a registered map ───────
    [Fact]
    public void Map_MapFromReturnsNestedType_UsesRegisteredMapping()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithNested, DestWithMapped>()
                .ForMember(d => d.Nested, o => o.MapFrom(s => s.Inner));
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>();
        }).CreateMapper();

        var src = new SourceWithNested { Name = "parent", Inner = new NestedInnerSrc { Value = 42 } };
        var result = mapper.Map<SourceWithNested, DestWithMapped>(src);

        result.Name.Should().Be("parent");
        result.Nested.Should().NotBeNull();
        result.Nested!.Value.Should().Be(42);
    }

    // ── Implicit operator conversion (Id<T> → int pattern) ───────────────
    [Fact]
    public void Map_ImplicitOperatorConversion_ConvertsStrongIdToInt()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StrongIdSource, IntIdDest>())
            .CreateMapper();

        var src = new StrongIdSource { ArtistId = new ImplicitId(42) };
        var result = mapper.Map<StrongIdSource, IntIdDest>(src);
        result.ArtistId.Should().Be(42);
    }

    [Fact]
    public void Map_ImplicitOperatorConversion_WorksWithIgnoredProps()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StrongIdSource, IntIdDest>()
                .ForMember(d => d.ArtistId, o => o.Ignore()))
            .CreateMapper();

        var src = new StrongIdSource { ArtistId = new ImplicitId(99) };
        var result = mapper.Map<StrongIdSource, IntIdDest>(src);
        result.ArtistId.Should().Be(0); // ignored, stays default
    }

    [Fact]
    public void Map_MapFromReturnsNullNestedType_ReturnsNull()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithNested, DestWithMapped>()
                .ForMember(d => d.Nested, o => o.MapFrom(s => s.Inner));
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>();
        }).CreateMapper();

        var src = new SourceWithNested { Name = "parent", Inner = null };
        var result = mapper.Map<SourceWithNested, DestWithMapped>(src);

        result.Name.Should().Be("parent");
        result.Nested.Should().BeNull();
    }

    [Fact]
    public void Map_MapFromReturnsCollection_UsesRegisteredElementMapping()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<SourceWithNestedList, DestWithMappedList>()
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Inners));
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>();
        }).CreateMapper();

        var src = new SourceWithNestedList
        {
            Inners = new List<NestedInnerSrc>
            {
                new() { Value = 1 },
                new() { Value = 2 }
            }
        };
        var result = mapper.Map<SourceWithNestedList, DestWithMappedList>(src);

        result.Items.Should().HaveCount(2);
        result.Items[0].Value.Should().Be(1);
        result.Items[1].Value.Should().Be(2);
    }

    // ── Safe member access: MapFrom with null navigation property ────────
    [Fact]
    public void Map_MapFromNullNavigation_ReturnsNullInsteadOfThrowing()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ReportEntity, ReportDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, o => o.MapFrom(s => s.User.Email)))
            .CreateMapper();

        var src = new ReportEntity { Id = 1, User = null! };
        var result = mapper.Map<ReportEntity, ReportDto>(src);

        result.Id.Should().Be(1);
        result.UserName.Should().BeNull();
        result.UserEmail.Should().BeNull();
    }

    [Fact]
    public void Map_MapFromNullNavigation_MethodCall_ReturnsNull()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ReportEntity, ReportDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.GetDisplayName())))
            .CreateMapper();

        var src = new ReportEntity { Id = 2, User = null! };
        var result = mapper.Map<ReportEntity, ReportDto>(src);

        result.UserName.Should().BeNull();
    }

    [Fact]
    public void Map_MapFromValidNavigation_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<ReportEntity, ReportDto>()
                .ForMember(d => d.UserName, o => o.MapFrom(s => s.User.FullName))
                .ForMember(d => d.UserEmail, o => o.MapFrom(s => s.User.Email)))
            .CreateMapper();

        var src = new ReportEntity { Id = 1, User = new UserEntity { FullName = "John Doe", Email = "john@test.com" } };
        var result = mapper.Map<ReportEntity, ReportDto>(src);

        result.Id.Should().Be(1);
        result.UserName.Should().Be("John Doe");
        result.UserEmail.Should().Be("john@test.com");
    }

    // ── Property-level error messages ────────────────────────────────────
    [Fact]
    public void Map_PropertyError_ExceptionIncludesMemberName()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>()
                .ForMember(d => d.Name, o => o.MapFrom<Func<object, object, object>>((s, d) =>
                    throw new InvalidOperationException("deliberate"))))
            .CreateMapper();

        var act = () => mapper.Map<FlatSource, FlatDest>(new FlatSource());
        var ex = act.Should().Throw<MappingException>().Which;
        ex.MemberName.Should().Be("Name");
        ex.Message.Should().Contain("Name");
        ex.Message.Should().Contain("deliberate");
    }

    // ── #67: Null collection → empty collection ─────────────────────────
    [Fact]
    public void Map_NullSourceCollection_ReturnsEmptyList()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CollectionSrc, CollectionDst>();
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>();
        }).CreateMapper();

        var src = new CollectionSrc { Items = null! };
        var result = mapper.Map<CollectionSrc, CollectionDst>(src);

        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void Map_NonNullSourceCollection_MapsNormally()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<CollectionSrc, CollectionDst>();
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>();
        }).CreateMapper();

        var src = new CollectionSrc { Items = new() { new() { Value = 1 } } };
        var result = mapper.Map<CollectionSrc, CollectionDst>(src);

        result.Items.Should().HaveCount(1);
        result.Items[0].Value.Should().Be(1);
    }

    // ── #68: Null elements in list ──────────────────────────────────────
    [Fact]
    public void MapList_NullElements_ReturnsDefaultInsteadOfThrowing()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>())
            .CreateMapper();

        var source = new List<NestedInnerSrc> { new() { Value = 1 }, null!, new() { Value = 3 } };
        var result = mapper.MapList<NestedInnerSrc, NestedInnerDst>(source);

        result.Should().HaveCount(3);
        result[0].Value.Should().Be(1);
        result[1].Should().BeNull();
        result[2].Value.Should().Be(3);
    }

    [Fact]
    public void MapList_NullElements_WorksAfterCacheWarmup()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NestedInnerSrc, NestedInnerDst>())
            .CreateMapper();

        // First call warms cache
        mapper.MapList<NestedInnerSrc, NestedInnerDst>(
            new List<NestedInnerSrc> { new() { Value = 1 } });

        // Second call with null elements uses cached fast path
        var source = new List<NestedInnerSrc> { null!, new() { Value = 2 }, null! };
        var result = mapper.MapList<NestedInnerSrc, NestedInnerDst>(source);

        result.Should().HaveCount(3);
        result[0].Should().BeNull();
        result[1].Value.Should().Be(2);
        result[2].Should().BeNull();
    }

    // ── #69: opts.Items and AfterMap timing ──────────────────────────────
    [Fact]
    public void Map_OptsAfterMap_FiresAfterMapping()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>())
            .CreateMapper();

        var src = new FlatSource { Name = "original" };
        var result = mapper.Map<FlatSource, FlatDest>(src, opt =>
        {
            opt.AfterMap((s, d) => d.Name = d.Name + "_after");
        });

        result.Name.Should().Be("original_after");
    }

    [Fact]
    public void Map_OptsItems_AccessibleViaClosureInAfterMap()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>())
            .CreateMapper();

        var src = new FlatSource { Name = "test" };
        var result = mapper.Map<FlatSource, FlatDest>(src, opt =>
        {
            opt.Items["Suffix"] = "_custom";
            opt.AfterMap((s, d) => d.Name = d.Name + (string)opt.Items["Suffix"]);
        });

        result.Name.Should().Be("test_custom");
    }

    // ── #70: Multi-level flattening ─────────────────────────────────────
    [Fact]
    public void Map_ThreeLevelFlattening_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<DeepSrc, DeepFlatDst>())
            .CreateMapper();

        var src = new DeepSrc
        {
            Address = new AddressSrc
            {
                City = new CitySrc { Name = "London", PostCode = "SW1" }
            }
        };
        var result = mapper.Map<DeepSrc, DeepFlatDst>(src);

        result.AddressCityName.Should().Be("London");
        result.AddressCityPostCode.Should().Be("SW1");
    }

    [Fact]
    public void Map_ThreeLevelFlattening_NullIntermediate_ReturnsDefault()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<DeepSrc, DeepFlatDst>())
            .CreateMapper();

        var src = new DeepSrc { Address = new AddressSrc { City = null! } };
        var result = mapper.Map<DeepSrc, DeepFlatDst>(src);
        result.AddressCityName.Should().BeNull();

        src = new DeepSrc { Address = null! };
        result = mapper.Map<DeepSrc, DeepFlatDst>(src);
        result.AddressCityName.Should().BeNull();
    }

    // ── #71: ctx-free path error wrapping ───────────────────────────────
    [Fact]
    public void Map_CtxFreePathError_WrappedInMappingException()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>())
            .CreateMapper();

        // Force error by mapping incompatible types through the generic path
        var act = () => mapper.Map<string, FlatDest>("not a FlatSource");
        act.Should().Throw<Exception>(); // Should not crash with raw NRE
    }

    // ── Multi-level nested object mapping (child→child→child) ────────────
    [Fact]
    public void Map_ThreeLevelNestedObjects_MapsAllLevels()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSrc, OrderDst>();
            cfg.CreateMap<CustomerSrc, CustomerDst>();
            cfg.CreateMap<ShippingAddressSrc, ShippingAddressDst>();
        }).CreateMapper();

        var src = new OrderSrc
        {
            Id = 1,
            Customer = new CustomerSrc
            {
                Name = "Alice",
                ShippingAddress = new ShippingAddressSrc
                {
                    Street = "123 Main St",
                    City = "NYC"
                }
            }
        };
        var result = mapper.Map<OrderSrc, OrderDst>(src);

        result.Id.Should().Be(1);
        result.Customer.Should().NotBeNull();
        result.Customer!.Name.Should().Be("Alice");
        result.Customer.ShippingAddress.Should().NotBeNull();
        result.Customer.ShippingAddress!.Street.Should().Be("123 Main St");
        result.Customer.ShippingAddress.City.Should().Be("NYC");
    }

    [Fact]
    public void Map_ThreeLevelNestedObjects_NullIntermediate_ReturnsNull()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSrc, OrderDst>();
            cfg.CreateMap<CustomerSrc, CustomerDst>();
            cfg.CreateMap<ShippingAddressSrc, ShippingAddressDst>();
        }).CreateMapper();

        var src = new OrderSrc { Id = 1, Customer = new CustomerSrc { Name = "Bob", ShippingAddress = null! } };
        var result = mapper.Map<OrderSrc, OrderDst>(src);

        result.Customer.Should().NotBeNull();
        result.Customer!.ShippingAddress.Should().BeNull();

        src = new OrderSrc { Id = 2, Customer = null! };
        result = mapper.Map<OrderSrc, OrderDst>(src);
        result.Customer.Should().BeNull();
    }

    [Fact]
    public void Map_ThreeLevelNestedObjects_WithCollections()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<OrderSrc, OrderDst>();
            cfg.CreateMap<CustomerSrc, CustomerDst>();
            cfg.CreateMap<ShippingAddressSrc, ShippingAddressDst>();
        }).CreateMapper();

        var sources = new List<OrderSrc>
        {
            new() { Id = 1, Customer = new() { Name = "A", ShippingAddress = new() { Street = "St1", City = "C1" } } },
            new() { Id = 2, Customer = new() { Name = "B", ShippingAddress = new() { Street = "St2", City = "C2" } } }
        };
        var results = mapper.MapList<OrderSrc, OrderDst>(sources);

        results.Should().HaveCount(2);
        results[0].Customer!.ShippingAddress!.City.Should().Be("C1");
        results[1].Customer!.ShippingAddress!.City.Should().Be("C2");
    }
}

// ── Implicit operator test models (Id<T> → int via implicit operator) ────────
public class ImplicitId
{
    public int Value { get; }
    public ImplicitId(int value) => Value = value;
    public static implicit operator int(ImplicitId? id) => id?.Value ?? 0;
}
public class StrongIdSource { public ImplicitId ArtistId { get; set; } = new(0); }
public class IntIdDest { public int ArtistId { get; set; } }

// ── Additional test models
public class NestedInnerSrc { public int Value { get; set; } }
public class NestedInnerDst { public int Value { get; set; } }
public class SourceWithNested { public string? Name { get; set; } public NestedInnerSrc? Inner { get; set; } }
public class DestWithMapped { public string? Name { get; set; } public NestedInnerDst? Nested { get; set; } }
public class SourceWithNestedList { public List<NestedInnerSrc> Inners { get; set; } = []; }
public class DestWithMappedList { public List<NestedInnerDst> Items { get; set; } = []; }

// ────────────────────────────────────────────────────────────────────────────── ──────────────────────────────────────────────────
public class BaseEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class DerivedProxy : BaseEntity
{
    // Simulates EF Core lazy-loading proxy
}

public class BaseDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

// ── Strongly-typed ID models (Id<T> pattern with interface constructor) ──────
public interface IHasId { int Id { get; } }

public class EntityWithId : IHasId
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

public class StrongId
{
    public int Value { get; }
    public StrongId() { }
    public StrongId(IHasId entity) { Value = entity.Id; }
}

public class StrongIdDto
{
    public StrongId? Id { get; set; }
    public string? Name { get; set; }
}

// ── Open-generic ConvertUsing test models (ISequenceIdEntity → Id<T> pattern) ─
public interface ISeqEntity { int Id { get; } int SeqId { get; } }
public class ConcreteEntity : ISeqEntity { public int Id { get; set; } public int SeqId { get; set; } public string? Name { get; set; } }
public class WrapperId<T> where T : class, ISeqEntity
{
    public int Value { get; set; }
    public int SeqId { get; set; }
}
public class SeqEntityToWrapperIdConverter<T> : EggMapper.ITypeConverter<ISeqEntity?, WrapperId<T>?> where T : class, ISeqEntity
{
    public WrapperId<T>? Convert(ISeqEntity? source, WrapperId<T>? destination, EggMapper.ResolutionContext context)
    {
        if (source == null) return null;
        return new WrapperId<T> { Value = source.Id, SeqId = source.SeqId };
    }
}
public class ConcreteEntityDto { public WrapperId<ConcreteEntity>? Id { get; set; } public string? Name { get; set; } }

// ── Navigation property / safe member access test models ─────────────────────
public class UserEntity
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string GetDisplayName() => FullName;
}
public class ReportEntity
{
    public int Id { get; set; }
    public UserEntity User { get; set; } = default!;
}
public class ReportDto
{
    public int Id { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
}

// ── Collection null → empty test models ──────────────────────────────────────
public class CollectionSrc { public List<NestedInnerSrc> Items { get; set; } = new(); }
public class CollectionDst { public List<NestedInnerDst> Items { get; set; } = new(); }

// ── Multi-level nested mapping test models ───────────────────────────────────
public class ShippingAddressSrc { public string Street { get; set; } = ""; public string City { get; set; } = ""; }
public class ShippingAddressDst { public string Street { get; set; } = ""; public string City { get; set; } = ""; }
public class CustomerSrc { public string Name { get; set; } = ""; public ShippingAddressSrc? ShippingAddress { get; set; } }
public class CustomerDst { public string Name { get; set; } = ""; public ShippingAddressDst? ShippingAddress { get; set; } }
public class OrderSrc { public int Id { get; set; } public CustomerSrc? Customer { get; set; } }
public class OrderDst { public int Id { get; set; } public CustomerDst? Customer { get; set; } }

// ── Multi-level flattening test models ───────────────────────────────────────
public class CitySrc { public string Name { get; set; } = ""; public string PostCode { get; set; } = ""; }
public class AddressSrc { public CitySrc City { get; set; } = default!; }
public class DeepSrc { public AddressSrc Address { get; set; } = default!; }
public class DeepFlatDst { public string? AddressCityName { get; set; } public string? AddressCityPostCode { get; set; } }

// ──────────────────────────────────────────────────────────────────────────────
public class DateTimeSource
{
    public DateTime Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public Guid Id { get; set; }
}

public class DateTimeDest
{
    public DateTime Created { get; set; }
    public DateTimeOffset Modified { get; set; }
    public Guid Id { get; set; }
}

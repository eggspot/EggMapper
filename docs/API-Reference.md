---
layout: default
title: API Reference
nav_order: 4
---

# API Reference

## Namespace `EggMapper`

---

### `MapperConfiguration`

The root configuration object. Construct once at startup; keep as a singleton.

```csharp
public sealed class MapperConfiguration
```

#### Constructor

```csharp
public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
```

Compiles all registered type maps into cached delegates during construction.

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateMapper()` | `IMapper` | Returns a mapper backed by the compiled cache |
| `AssertConfigurationIsValid()` | `void` | Throws if any destination property is unmapped |

---

### `IMapper`

The runtime mapping interface. Resolve from DI or call `config.CreateMapper()`.

```csharp
public interface IMapper
```

#### Methods

| Signature | Description |
|-----------|-------------|
| `TDst Map<TSrc, TDst>(TSrc source)` | Map source to a new destination instance |
| `TDst Map<TSrc, TDst>(TSrc source, TDst destination)` | Map source into an existing destination instance |
| `TDst Map<TDst>(object source)` | Map source (type inferred at runtime) to `TDst` |

---

### `IMapperConfigurationExpression`

Fluent interface passed to the `MapperConfiguration` constructor callback.

| Method | Description |
|--------|-------------|
| `CreateMap<TSrc, TDst>()` | Register a type map; returns `IMappingExpression<TSrc, TDst>` |
| `AddProfile<TProfile>()` | Register all maps in a `Profile` subclass |
| `AddProfile(Profile profile)` | Register a pre-constructed `Profile` instance |
| `AddProfiles(params Assembly[] assemblies)` | Scan assemblies and register all `Profile` subclasses |

---

### `IMappingExpression<TSrc, TDst>`

Fluent builder returned by `CreateMap<TSrc, TDst>()`.

| Method | Description |
|--------|-------------|
| `ForMember(dst => dst.Prop, opt => opt.MapFrom(…))` | Custom source expression |
| `ForMember(dst => dst.Prop, opt => opt.Ignore())` | Skip destination property |
| `ForMember(dst => dst.Prop, opt => opt.Condition(s => …))` | Map only when predicate is true |
| `ForMember(dst => dst.Prop, opt => opt.PreCondition(s => …))` | Skip source read when predicate is false |
| `ForMember(dst => dst.Prop, opt => opt.NullSubstitute(value))` | Use fallback when source is null |
| `ForPath(dst => dst.A.B.Prop, opt => opt.MapFrom(…))` | Map to a deeply nested destination path |
| `ReverseMap()` | Also register the inverse mapping |
| `BeforeMap(Action<TSrc, TDst>)` | Hook called before any property is mapped |
| `AfterMap(Action<TSrc, TDst>)` | Hook called after all properties are mapped |
| `MaxDepth(int depth)` | Limit recursion depth for self-referencing types |
| `IncludeBase<TBaseSrc, TBaseDst>()` | Inherit the base type's mapping rules |

---

### `Profile`

Base class for grouping related maps.

```csharp
public abstract class Profile
```

Call `CreateMap<TSrc, TDst>()` inside your constructor to register maps. The API is identical to `IMapperConfigurationExpression`.

---

### `MappingException`

Thrown when a mapping fails at runtime (e.g. unsupported type conversion, null reference in a non-nullable path).

```csharp
public sealed class MappingException : Exception
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SourceType` | `Type` | The source type being mapped |
| `DestinationType` | `Type` | The destination type |
| `InnerException` | `Exception?` | The underlying exception |

---

## Namespace `Microsoft.Extensions.DependencyInjection`

> Requires the `EggMapper.DependencyInjection` package.

### `EggMapperServiceCollectionExtensions`

| Extension method | Description |
|------------------|-------------|
| `AddEggMapper(params Assembly[])` | Scan assemblies for `Profile` subclasses and register `IMapper` as singleton |
| `AddEggMapper(Action<IMapperConfigurationExpression>)` | Inline configuration without profiles |

---
layout: default
title: Configuration
parent: Guide
nav_order: 1
description: "EggMapper configuration — MapperConfiguration, CreateMap, validation, thread safety."
---

# Configuration
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## `MapperConfiguration`

`MapperConfiguration` is the **entry point** for EggMapper. You construct it once (typically at startup) and keep a single instance for the lifetime of the application.

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg =>
{
    // Register maps here
    cfg.CreateMap<Source, Destination>();
});
```

`MapperConfiguration` is immutable after construction. It is safe to call `CreateMapper()`, `Map()`, and `BuildProjection()` concurrently from any number of threads.
{: .warning }

---

## Registering Maps

### Basic map

```csharp
cfg.CreateMap<Source, Destination>();
```

By convention, properties with identical names (case-insensitive) are mapped automatically. No configuration needed for matching property names.

### With custom member mapping

```csharp
cfg.CreateMap<Order, OrderDto>()
    .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName))
    .ForMember(d => d.TotalWithTax, o => o.MapFrom(s => s.Total * 1.1m))
    .ForMember(d => d.InternalNotes, o => o.Ignore());
```

### Reverse map

```csharp
cfg.CreateMap<Source, Destination>().ReverseMap();
// Registers both Source -> Destination and Destination -> Source
```

### Multiple maps

```csharp
cfg.CreateMap<Customer, CustomerDto>();
cfg.CreateMap<Customer, CustomerSummaryDto>();
cfg.CreateMap<Address,  AddressDto>();
cfg.CreateMap<Order,    OrderDto>();
cfg.CreateMap<Order,    OrderSummaryDto>();
```

### Open generic maps

```csharp
cfg.CreateMap(typeof(Result<>), typeof(ResultDto<>));
cfg.CreateMap(typeof(PagedList<>), typeof(PagedListDto<>));
```

---

## Adding Profiles

Group related maps in a [`Profile`](Profiles) class and register the entire profile at once:

```csharp
cfg.AddProfile<OrderProfile>();
cfg.AddProfile<CustomerProfile>();
```

Or scan an assembly for all profiles:

```csharp
cfg.AddProfiles(typeof(OrderProfile).Assembly);
```

Assembly scanning is recommended for large projects. It automatically discovers and registers all `Profile` subclasses.
{: .note }

---

## Creating a Mapper

```csharp
IMapper mapper = config.CreateMapper();
```

`CreateMapper()` returns a lightweight `IMapper` instance backed by the compiled delegate cache. You may call it multiple times; each call returns a new wrapper around the same shared cache.

---

## Configuration Validation

Validate that every destination property has a source mapping (catches typos and missing maps early):

```csharp
config.AssertConfigurationIsValid();
```

Call this in unit tests or at application startup to surface misconfiguration immediately.

```csharp
// Throws InvalidOperationException if any destination property is unmapped
config.AssertConfigurationIsValid();
```

### What validation checks

- Every destination property must have either:
  - A matching source property (by name, case-insensitive)
  - A custom `MapFrom` expression
  - A flattening match (e.g., `AddressCity` matches `Address.City`)
  - An explicit `Ignore()` call
- Nested type maps must be registered
- Constructor parameters must match source properties

### Recommended: validate in a unit test

```csharp
[Fact]
public void AllMappings_ShouldBeValid()
{
    var config = new MapperConfiguration(cfg =>
        cfg.AddProfiles(typeof(OrderProfile).Assembly));

    config.AssertConfigurationIsValid();
}
```

---

## `IMapperConfigurationExpression` Options

| Method | Description |
|--------|-------------|
| `CreateMap<TSrc, TDst>()` | Register a new type map and return the fluent builder |
| `CreateMap(Type src, Type dst)` | Register an open-generic type map |
| `AddProfile<TProfile>()` | Register all maps declared in a `Profile` subclass |
| `AddProfile(profile)` | Register a pre-constructed `Profile` instance |
| `AddProfiles(assemblies)` | Scan assemblies and register all `Profile` subclasses |

---

## Configuration Patterns

### Small application — inline configuration

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Product, ProductDto>();
    cfg.CreateMap<Order, OrderDto>()
        .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName));
});
```

### Medium application — profiles

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<OrderProfile>();
    cfg.AddProfile<CustomerProfile>();
    cfg.AddProfile<ProductProfile>();
});
```

### Large application — assembly scanning

```csharp
var config = new MapperConfiguration(cfg =>
    cfg.AddProfiles(
        typeof(OrderProfile).Assembly,        // Core domain
        typeof(ReportProfile).Assembly));     // Reporting module
```

### Multi-module application

```csharp
// Each module defines its own profiles
builder.Services.AddEggMapper(
    typeof(OrderModule.OrderProfile).Assembly,
    typeof(InventoryModule.ProductProfile).Assembly,
    typeof(ReportingModule.ReportProfile).Assembly);
```

---

## Thread Safety

| Scenario | Safe? |
|----------|-------|
| Construct `MapperConfiguration` on one thread | Yes |
| Call `CreateMapper()` concurrently | Yes |
| Call `IMapper.Map()` concurrently | Yes |
| Call `IMapper.MapList()` concurrently | Yes |
| Call `IMapper.Patch()` concurrently | Yes |
| Use `ProjectTo()` concurrently | Yes |
| Add maps after construction | Not supported |

All mapping operations are thread-safe after construction. The compiled delegate cache is immutable and shared across all mapper instances.

---

## Common Pitfalls

- **Constructing `MapperConfiguration` per request** — This recompiles all expression trees every time. Always keep it as a singleton.
- **Adding maps after construction** — Not supported. All maps must be registered in the constructor callback or in profiles.
- **Not calling `AssertConfigurationIsValid()`** — Missing maps or typos in property names silently leave destination properties at their default values. Always validate in tests.
- **Registering the same type pair twice** — The second `CreateMap<S,D>()` overwrites the first. This is usually a mistake. Keep each type pair in a single profile.

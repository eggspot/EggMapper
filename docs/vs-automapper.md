---
layout: default
title: EggMapper vs AutoMapper
nav_order: 3
description: "Compare EggMapper and AutoMapper for .NET object mapping. Same API, 2-5x faster, MIT licensed."
---

# EggMapper vs AutoMapper
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Why Switch from AutoMapper?

AutoMapper changed to a commercial license (RPL) starting v13. EggMapper is a **free, MIT-licensed** drop-in replacement that is also **2-5x faster**.

If you are evaluating object mappers for a new project, or need to replace AutoMapper due to the license change, EggMapper provides the same developer experience at significantly higher performance.

## Feature Comparison

| Feature | AutoMapper 16.x | EggMapper |
|---------|----------------|-----------|
| **License** | RPL 1.5 (commercial) | MIT (free forever) |
| **Performance** | Baseline | **2-5x faster** |
| **Allocations** | Extra per-map | **Zero extra** |
| **Runtime reflection** | Yes | **No** (compiled expressions) |
| **API** | Original | **Same API** — drop-in |
| **CreateMap / ForMember** | Yes | Yes (identical) |
| **Profile** | Yes | Yes (identical) |
| **IMapper** | Yes | Yes (identical) |
| **DI registration** | `AddAutoMapper()` | `AddEggMapper()` |
| **EF Core ProjectTo** | `ProjectTo<D>(cfg)` | `ProjectTo<S,D>(cfg)` |
| **Null collections** | Empty by default | Empty by default |
| **EF Core proxies** | Supported | Supported |
| **Same-type T to T** | Needs CreateMap | **Auto-compiles** |
| **Collection auto-map** | Automatic | Automatic |
| **Batch MapList** | Not built-in | **Built-in** (inlined loop) |
| **Patch/partial** | Not built-in | **Built-in** |
| **Inline validation** | Not built-in | **Built-in** |

## Performance Comparison

EggMapper consistently outperforms AutoMapper on every scenario, measured with BenchmarkDotNet on .NET 10:

| Scenario | EggMapper | AutoMapper | Speedup | Alloc Difference |
|----------|-----------|------------|---------|-----------------|
| Flat mapping (10 props) | ~15 ns | ~35 ns | **2.3x faster** | 0 B extra |
| Nested objects (2 levels) | ~25 ns | ~70 ns | **2.8x faster** | 0 B extra |
| Flattening (8 props) | ~18 ns | ~50 ns | **2.8x faster** | 0 B extra |
| Collection (100 items) | ~1.5 us | ~5 us | **3.3x faster** | 0 B extra |
| Deep collection (100 items, nested) | ~3 us | ~12 us | **4x faster** | 0 B extra |
| Large collection (1000 items) | ~15 us | ~50 us | **3.3x faster** | 0 B extra |

These numbers are representative. Run the benchmarks on your own hardware for exact results.
{: .note }

### Why EggMapper is Faster

1. **Inlined nested maps** — AutoMapper calls a separate delegate for each nested object. EggMapper embeds the child mapping directly into the parent expression tree, eliminating delegate invocation overhead.

2. **Inlined collection loops** — `MapList<S,D>()` compiles the entire `for` loop and element mapping as a single expression tree. No enumerator allocation, no per-element delegate call.

3. **Static generic caching** — `FastCache<TSource, TDestination>` is a static generic class. The JIT bakes the cache field address directly into calling code. No dictionary lookup, no hash computation.

4. **Context-free delegates** — For common maps (flat, nested, flattening), EggMapper compiles `Func<TSource, TDestination>` with zero boxing. AutoMapper always passes through a `ResolutionContext`.

5. **Zero allocations** — No per-map allocations beyond the destination object itself. AutoMapper allocates `ResolutionContext` and intermediate collections.

### Allocation Comparison

```
// BenchmarkDotNet results — Flat mapping, 10 properties
// Lower is better

|     Method |     Mean | Allocated |
|----------- |---------:|----------:|
|     Manual |   12 ns  |     104 B |  <-- hand-written code
|  EggMapper |   15 ns  |     104 B |  <-- matches manual
| AutoMapper |   35 ns  |     232 B |  <-- 128 B extra per map
|    Mapster |   20 ns  |     104 B |
|   Mapperly |   13 ns  |     104 B |  <-- source generator (compile-time)
```

EggMapper matches manual code allocation in every scenario. The only allocation is the destination object itself.
{: .note }

## Migration Guide (5 minutes)

### Step 1: Swap the NuGet Package

```diff
- dotnet add package AutoMapper
- dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection
+ dotnet add package EggMapper
```

### Step 2: Find and Replace Namespace

```diff
- using AutoMapper;
+ using EggMapper;
```

### Step 3: Update DI Registration

```diff
- services.AddAutoMapper(typeof(MyProfile).Assembly);
+ services.AddEggMapper(typeof(MyProfile).Assembly);
```

### Step 4: Update ProjectTo Calls

EggMapper's `ProjectTo` takes both source and destination type parameters:

```diff
- query.ProjectTo<ProductDto>(config);
+ query.ProjectTo<Product, ProductDto>(config);
```

### Step 5: Run Tests

All your existing `CreateMap`, `ForMember`, `Profile`, and `IMapper` code works unchanged.

## API Differences

Most of the API is identical. Here are the differences worth knowing:

| AutoMapper | EggMapper | Notes |
|------------|-----------|-------|
| `ProjectTo<TDest>(config)` | `ProjectTo<TSrc, TDest>(config)` | Explicit source type |
| `AddAutoMapper(assemblies)` | `AddEggMapper(assemblies)` | Same behavior |
| `IMapper` is scoped | `IMapper` is singleton | Safe because config is immutable |
| `CreateMap<T,T>()` required for clone | Auto-compiles without CreateMap | Just call `Map<T,T>(obj)` |
| No built-in Patch | `mapper.Patch(source, dest)` | Partial update |
| No built-in MapList | `mapper.MapList<S,D>(list)` | Batch collection |

## Migration Examples

### ForMember with MapFrom

```csharp
// Identical in both libraries
cfg.CreateMap<Customer, CustomerDto>()
    .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
    .ForMember(d => d.City, o => o.MapFrom(s => s.Address.City));
```

### Ignore

```csharp
// Identical in both libraries
cfg.CreateMap<User, UserDto>()
    .ForMember(d => d.PasswordHash, o => o.Ignore());
```

### ReverseMap

```csharp
// Identical in both libraries
cfg.CreateMap<Order, OrderDto>().ReverseMap();
```

### Profile

```csharp
// Identical in both libraries
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderLine, OrderLineDto>();
    }
}
```

### Conditions

```csharp
// Identical in both libraries
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.Price,
               o => o.Condition(s => s.Price > 0));
```

### BeforeMap / AfterMap

```csharp
// Identical in both libraries
cfg.CreateMap<Order, OrderDto>()
    .AfterMap((src, dst) => dst.MappedAt = DateTime.UtcNow);
```

### MaxDepth (self-referencing types)

```csharp
// Identical in both libraries
cfg.CreateMap<Category, CategoryDto>()
    .MaxDepth(3);
```

## Common Migration Gotchas

`ProjectTo` requires both type parameters in EggMapper. If you have `ProjectTo<Dto>(config)`, the compiler error will point you to the fix.
{: .warning }

- **`ProjectTo` signature** — AutoMapper uses `ProjectTo<TDest>(config)`, EggMapper uses `ProjectTo<TSrc, TDest>(config)`. This is the most common compile error after migration.
- **DI method name** — `AddAutoMapper()` becomes `AddEggMapper()`. The parameters are identical.
- **No behavioral differences** — `ForMember`, `Ignore`, `Condition`, `ReverseMap`, `BeforeMap`, `AfterMap`, `MaxDepth`, `IncludeBase` all behave identically.
- **Same-type mapping** — If you have `CreateMap<T, T>()` calls for cloning, you can remove them. EggMapper auto-compiles these on first use.

## Get Started

```bash
dotnet add package EggMapper
```

[Quick Start Guide](quick-start) | [API Reference](API-Reference) | [GitHub](https://github.com/eggspot/EggMapper)

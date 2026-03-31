---
layout: default
title: Performance
nav_order: 6
description: "EggMapper performance — benchmarks, allocation analysis, optimization techniques, and tuning tips."
---

# Performance
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## How EggMapper Achieves High Performance

EggMapper is the **fastest .NET runtime object-to-object mapper**, achieving near-manual mapping speed through these techniques:

1. **Compile once, run many times** — `MapperConfiguration` compiles expression-tree delegates at construction time. Every subsequent `Map()` call is a direct delegate invocation with no reflection.
2. **Context-free typed delegates** — For flat and nested maps, EggMapper compiles `Func<TSource, TDestination>` delegates with zero boxing. Nested object mappings are **inlined directly** into the parent expression tree.
3. **Static generic caching** — `FastCache<TSource, TDestination>` eliminates dictionary lookups after the first call for each type pair.
4. **Inlined collection loops** — `MapList<>()` uses compiled `Func<IList<TSource>, List<TDestination>>` delegates where the entire loop + element mapping is a single expression tree.
5. **Zero extra allocations** — EggMapper matches hand-written code allocation in every scenario.

---

## Why EggMapper is Faster Than AutoMapper

### 1. Inlined Nested Maps

AutoMapper maps nested objects by calling a separate delegate for each child property. This means every nested object incurs delegate invocation overhead plus `ResolutionContext` management.

EggMapper **inlines** the entire child mapping directly into the parent expression tree. The compiled delegate for `Order -> OrderDto` contains the code for `Customer -> CustomerDto` and `Address -> AddressDto` directly — no intermediate calls.

```
// What AutoMapper compiles (conceptual):
dst.Customer = _customerMapper(src.Customer, ctx);  // delegate call
dst.Address  = _addressMapper(src.Address, ctx);    // delegate call

// What EggMapper compiles (conceptual):
dst.Customer = new CustomerDto {
    Id = src.Customer.Id,
    Name = src.Customer.Name       // inlined directly
};
dst.Address = new AddressDto {
    City = src.Address.City,       // inlined directly
    Street = src.Address.Street
};
```

### 2. Inlined Collection Loops

`MapList<S,D>()` compiles the entire collection loop as a single expression tree:

```
// Compiled delegate (conceptual):
(IList<Order> src) => {
    var result = new List<OrderDto>(src.Count);
    for (int i = 0; i < src.Count; i++) {
        var item = src[i];
        result.Add(new OrderDto {
            Id = item.Id,
            CustomerName = item.Customer.Name,  // nested mapping inlined
            Total = item.Total
        });
    }
    return result;
}
```

No enumerator allocation. No per-element delegate call. The JIT sees a tight `for` loop with direct property access.

### 3. Static Generic Caching

```csharp
// EggMapper: zero-cost lookup via static generic class
static class FastCache<TSource, TDestination>
{
    public static volatile CacheEntry? Entry;
}

// The JIT bakes the field address directly into calling code.
// No dictionary, no hash, no key comparison.
```

### 4. Context-Free Delegates

For the majority of maps (flat, nested, flattening), EggMapper compiles `Func<TSource, TDestination>` — a plain function with no context parameter, no boxing, no allocation beyond the destination object.

AutoMapper always passes through `ResolutionContext`, which adds overhead even when context features are not used.

---

## Allocation Analysis

EggMapper matches hand-written code allocation. The **only** allocation is the destination object itself:

| Scenario | Manual | EggMapper | AutoMapper | Mapster |
|----------|--------|-----------|------------|---------|
| Flat (10 props) | 104 B | **104 B** | 232 B | 104 B |
| Nested (2 objects) | 248 B | **248 B** | 504 B | 248 B |
| Collection (100 items) | 10,824 B | **10,824 B** | 14,424 B | 10,824 B |
| Deep collection (100 items, nested) | 34,424 B | **34,424 B** | 52,824 B | 34,424 B |

The "extra" allocations in AutoMapper come from `ResolutionContext` management and intermediate delegate infrastructure. EggMapper eliminates all of this at compile time.

These numbers are representative of .NET 10 x64 builds. Exact values vary by runtime and architecture.
{: .note }

---

## Benchmark Setup

The benchmark suite lives in `src/EggMapper.Benchmarks/` and uses [BenchmarkDotNet](https://benchmarkdotnet.org/).

Each class compares **six mappers** against the same **manual** (hand-written) baseline:

| Benchmark class | Scenario |
|---|---|
| `FlatMappingBenchmark` | 10-property flat object |
| `FlatteningBenchmark` | Flattening 2 nested objects into 8 properties |
| `DeepTypeBenchmark` | Object with two nested address objects |
| `ComplexTypeBenchmark` | Nested object + `List<T>` children |
| `CollectionBenchmark` | `List<T>` with 100 elements |
| `DeepCollectionBenchmark` | 100 elements with 2 nested objects each |
| `LargeCollectionBenchmark` | `List<T>` with 1,000 elements |
| `StartupBenchmark` | Configuration / compilation time |

**Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper.

---

## Running Benchmarks Locally

```bash
cd src/EggMapper.Benchmarks

# All benchmarks on .NET 10 (recommended)
dotnet run -c Release -f net10.0 -- --filter '*'

# Single benchmark class
dotnet run -c Release -f net10.0 -- --filter '*FlatMapping*'

# Export to markdown + JSON
dotnet run -c Release -f net10.0 -- --filter '*' --exporters markdown json

# Faster CI-style run (fewer iterations)
dotnet run -c Release -f net10.0 -- --filter '*' --job short
```

Results are written to `BenchmarkDotNet.Artifacts/results/`.

---

## CI Benchmark Results

Benchmarks run automatically on every push to `main` and on every pull request via the [Benchmarks workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml).

- **Pull requests** receive a detailed comment with all tables, system info, and column descriptions.
- **Main branch** — the `README.md` Performance section is updated in-place with the latest tables.

---

## Performance Targets

| Scenario | Target |
|---|---|
| Flat mapping | Faster than Mapster |
| Deep / nested mapping | Faster than Mapster |
| Flattening | Faster than Mapster |
| Collection (100 items) | Within 10% of Mapster |
| All scenarios | 1.5-2.5x faster than AutoMapper |
| All scenarios | Zero extra allocations vs manual |

> A **lower ratio** is better. `Ratio = 1.00` equals the hand-written Manual baseline.

---

## Tips for Best Performance in Your Application

### Use a Singleton `MapperConfiguration`

Never construct `MapperConfiguration` per-request. The constructor compiles expression trees for every registered map. In DI, `AddEggMapper()` registers it as a singleton automatically.
{: .warning }

### Prefer Generic `Map<TSrc, TDst>()` Over `Map<TDst>(object)`

```csharp
// Fast: uses static generic cache (zero dict lookup)
var dto = mapper.Map<Order, OrderDto>(order);

// Slower: requires GetType() + dictionary lookup
var dto = mapper.Map<OrderDto>((object)order);
```

The generic overload uses `FastCache<TSource, TDestination>` — a static generic class where the JIT bakes the cache field address directly into the calling code. The non-generic overload must resolve the source type at runtime.

### Use `MapList<TSrc, TDst>()` for Collections

```csharp
// Fast: fully inlined compiled loop
List<OrderDto> dtos = mapper.MapList<Order, OrderDto>(orders);

// Slower: per-element delegate invocation
var dtos = orders.Select(o => mapper.Map<Order, OrderDto>(o)).ToList();
```

`MapList` compiles the entire loop as a single expression tree. For 100+ item collections, it is measurably faster.

### Use `ProjectTo` for Read-Only Queries

```csharp
// Best: SQL does the projection, no entity materialization
var dtos = await db.Orders
    .ProjectTo<Order, OrderDto>(config)
    .ToListAsync();

// Worse: loads full entities into memory, then maps
var entities = await db.Orders.ToListAsync();
var dtos = mapper.MapList<Order, OrderDto>(entities);
```

`ProjectTo` pushes the mapping into the SQL query. Only the columns needed for the DTO are selected. No entity tracking overhead.

### Register All Maps Upfront

Discovered maps compiled lazily still pay a one-time cost on first use. Register everything in profiles or the configuration callback to front-load compilation at startup.

### Validate in Tests

```csharp
[Fact]
public void MappingConfiguration_IsValid()
{
    var config = new MapperConfiguration(cfg =>
        cfg.AddProfiles(typeof(OrderProfile).Assembly));

    config.AssertConfigurationIsValid();
}
```

This ensures every map is exercised during the test run. It also catches missing maps and typos in property names before production.

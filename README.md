# 🥚 EggMapper

> **The fastest .NET runtime object-to-object mapper** — forked from AutoMapper's last open-source release, rebuilt for maximum performance. Drop-in replacement with the same API, 1.5–5× faster.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

📖 **[Full documentation →](https://github.com/eggspot/EggMapper/wiki)**

## Overview

**EggMapper** started as a fork of AutoMapper's last open-source release and was completely rewritten for performance. It keeps the **same familiar API** — `MapperConfiguration`, `CreateMap`, `ForMember`, `Profile`, `IMapper` — so you can switch from AutoMapper with minimal code changes. Under the hood, it compiles expression-tree delegates with inlined nested maps, typed collection loops, and static generic caching, achieving **zero reflection at map-time**, **zero extra allocations**, and near-manual mapping speed.

### Migrating from AutoMapper?

EggMapper is a **drop-in replacement**. In most cases, you only need to:

1. Replace `using AutoMapper;` with `using EggMapper;`
2. Replace `services.AddAutoMapper(...)` with `services.AddEggMapper(...)`

The same `CreateMap<>()`, `ForMember()`, `ReverseMap()`, `Profile`, and `IMapper` APIs work identically.

### Why EggMapper?

- 🚀 **Faster than Mapster** on flat, flattening, deep, and complex mappings
- 🔥 **1.5–5× faster than AutoMapper** across all scenarios
- 🎯 **Zero extra allocations** — matches hand-written code exactly
- 🔁 **Drop-in AutoMapper replacement** — same fluent API, same patterns
- 🧩 **Full feature set** — profiles, `ForMember`, `ReverseMap`, nested types, collections, DI, and more
- 🪶 **Lightweight** — no runtime reflection, no unnecessary allocations
- 📖 **MIT licensed** — free for commercial use, forever

## Installation

```bash
dotnet add package EggMapper
```

DI support (`AddEggMapper`) is included in the main package — no separate package needed.

## Quick Start

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg => {
    cfg.CreateMap<Source, Destination>();
});

var mapper = config.CreateMapper();
var dest = mapper.Map<Destination>(source);
```

## With Profiles

```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.AddressLine, opt => opt.MapFrom(s => s.Address.Street));
    }
}

var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
var mapper = config.CreateMapper();
```

## Dependency Injection

```csharp
// In Program.cs
builder.Services.AddEggMapper(typeof(MyProfile).Assembly);

// In your service
public class MyService(IMapper mapper) { ... }
```

## Performance

<!-- PERF_TIMESTAMP_START -->
> ⏱ **Last updated:** 2026-03-25 05:11 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.25 ns | **47.05 ns (3.1×)** | 28.29 ns (1.9×) | 81.68 ns (5.4×) | 15.04 ns (1.0×) |
| **Flattening** | 19.05 ns | **29.12 ns (1.5×)** | 47.95 ns (2.5×) | 88.51 ns (4.7×) | 23.54 ns (1.2×) |
| **Deep (2 nested)** | 53.43 ns | **64.25 ns (1.2×)** | 68.98 ns (1.3×) | 120.89 ns (2.3×) | 49.40 ns (0.9×) |
| **Complex (nest+coll)** | 70.54 ns | **90.88 ns (1.3×)** | 97.81 ns (1.4×) | 157.89 ns (2.2×) | 70.17 ns (1.0×) |
| **Collection (100)** | 1.732 μs | **1.741 μs (1.0×)** | 1.725 μs (1.0×) | 2.344 μs (1.4×) | 1.861 μs (1.1×) |
| **Deep Coll (100)** | 5.311 μs | **5.541 μs (1.0×)** | 5.766 μs (1.1×) | 6.509 μs (1.2×) | 5.346 μs (1.0×) |
| **Large Coll (1000)** | 17.82 μs | **17.68 μs (1.0×)** | 17.24 μs (1.0×) | 21.39 μs (1.2×) | 18.73 μs (1.1×) |
<!-- SUMMARY_TABLE_END -->

**\*** *Mapperly is a compile-time source generator — it produces code equivalent to hand-written mapping. EggMapper is the fastest **runtime** mapper.*

**Allocations:** EggMapper matches manual allocation exactly in every scenario (zero extra bytes).

Run the benchmarks yourself:

```bash
cd src/EggMapper.Benchmarks
dotnet run --configuration Release -f net10.0 -- --filter * --exporters json markdown
```

<!-- BENCHMARK_RESULTS_START -->

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method               | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual               |  15.25 ns | 0.207 ns | 0.184 ns |  15.04 ns |  15.20 ns |  15.56 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  47.05 ns | 0.557 ns | 0.521 ns |  46.26 ns |  47.18 ns |  47.91 ns |  3.09 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  81.68 ns | 0.311 ns | 0.276 ns |  80.98 ns |  81.74 ns |  82.07 ns |  5.36 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.29 ns | 0.284 ns | 0.266 ns |  27.92 ns |  28.24 ns |  28.73 ns |  1.86 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.04 ns | 0.138 ns | 0.123 ns |  14.84 ns |  15.02 ns |  15.32 ns |  0.99 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 482.63 ns | 2.525 ns | 2.362 ns | 480.00 ns | 481.57 ns | 487.65 ns | 31.66 |    0.40 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.09 ns | 0.171 ns | 0.160 ns |  14.84 ns |  15.08 ns |  15.35 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.55 ns | 0.215 ns | 0.190 ns |  15.27 ns |  15.60 ns |  15.89 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.05 ns | 0.239 ns | 0.223 ns |  18.73 ns |  18.98 ns |  19.49 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.12 ns | 0.147 ns | 0.137 ns |  28.94 ns |  29.07 ns |  29.39 ns |  1.53 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  88.51 ns | 0.525 ns | 0.491 ns |  87.70 ns |  88.57 ns |  89.08 ns |  4.65 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.95 ns | 0.427 ns | 0.400 ns |  47.24 ns |  47.91 ns |  48.70 ns |  2.52 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.54 ns | 0.153 ns | 0.143 ns |  23.27 ns |  23.56 ns |  23.78 ns |  1.24 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 532.19 ns | 1.896 ns | 1.773 ns | 529.93 ns | 531.76 ns | 535.88 ns | 27.95 |    0.33 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.43 ns | 1.111 ns | 0.927 ns |  52.28 ns |  53.26 ns |  55.72 ns |  1.00 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.25 ns | 0.695 ns | 0.650 ns |  63.45 ns |  64.01 ns |  65.41 ns |  1.20 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 120.89 ns | 0.984 ns | 0.921 ns | 119.61 ns | 120.77 ns | 122.32 ns |  2.26 |    0.04 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  68.98 ns | 0.775 ns | 0.605 ns |  67.71 ns |  69.11 ns |  69.88 ns |  1.29 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.40 ns | 0.747 ns | 0.698 ns |  48.37 ns |  49.22 ns |  50.36 ns |  0.92 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 509.43 ns | 3.475 ns | 3.251 ns | 505.64 ns | 508.45 ns | 514.80 ns |  9.54 |    0.17 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  70.54 ns | 1.431 ns | 1.339 ns |  68.78 ns |  70.33 ns |  73.06 ns |  1.00 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  90.88 ns | 1.097 ns | 1.026 ns |  89.52 ns |  91.12 ns |  92.35 ns |  1.29 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 157.89 ns | 3.063 ns | 3.404 ns | 152.73 ns | 158.32 ns | 164.23 ns |  2.24 |    0.06 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  97.81 ns | 1.263 ns | 1.181 ns |  95.78 ns |  97.75 ns |  99.55 ns |  1.39 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.17 ns | 0.564 ns | 0.500 ns |  69.46 ns |  70.10 ns |  71.13 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 556.44 ns | 2.639 ns | 2.339 ns | 551.84 ns | 556.09 ns | 559.98 ns |  7.89 |    0.15 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.732 μs | 0.0150 μs | 0.0140 μs | 1.715 μs | 1.726 μs | 1.762 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.741 μs | 0.0189 μs | 0.0168 μs | 1.717 μs | 1.746 μs | 1.765 μs |  1.01 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.344 μs | 0.0259 μs | 0.0242 μs | 2.313 μs | 2.335 μs | 2.400 μs |  1.35 |    0.02 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.725 μs | 0.0222 μs | 0.0207 μs | 1.680 μs | 1.727 μs | 1.760 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.861 μs | 0.0248 μs | 0.0232 μs | 1.807 μs | 1.856 μs | 1.898 μs |  1.07 |    0.02 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.667 μs | 0.0510 μs | 0.0524 μs | 2.548 μs | 2.687 μs | 2.728 μs |  1.54 |    0.03 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.311 μs | 0.0792 μs | 0.0702 μs | 5.119 μs | 5.334 μs | 5.395 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.541 μs | 0.0461 μs | 0.0431 μs | 5.467 μs | 5.543 μs | 5.609 μs |  1.04 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.509 μs | 0.1033 μs | 0.0916 μs | 6.375 μs | 6.513 μs | 6.712 μs |  1.23 |    0.02 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.766 μs | 0.0587 μs | 0.0520 μs | 5.676 μs | 5.767 μs | 5.860 μs |  1.09 |    0.02 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.346 μs | 0.0429 μs | 0.0381 μs | 5.275 μs | 5.351 μs | 5.411 μs |  1.01 |    0.01 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.133 μs | 0.0556 μs | 0.0520 μs | 5.043 μs | 5.127 μs | 5.204 μs |  0.97 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.82 μs | 0.128 μs | 0.119 μs | 17.62 μs | 17.81 μs | 17.99 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.68 μs | 0.200 μs | 0.167 μs | 17.21 μs | 17.68 μs | 17.86 μs |  0.99 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.39 μs | 0.320 μs | 0.300 μs | 21.02 μs | 21.30 μs | 22.04 μs |  1.20 |    0.02 |    4 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.24 μs | 0.283 μs | 0.251 μs | 16.91 μs | 17.18 μs | 17.85 μs |  0.97 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.73 μs | 0.373 μs | 0.415 μs | 18.04 μs | 18.63 μs | 19.31 μs |  1.05 |    0.02 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.14 μs | 0.184 μs | 0.164 μs | 19.87 μs | 20.16 μs | 20.39 μs |  1.13 |    0.01 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,041.899 μs | 3.4014 μs | 2.6556 μs | 1,037.635 μs | 1,041.777 μs | 1,045.791 μs | 1.000 |    3 | 3.9063 | 1.9531 |  93.75 KB |        1.00 |
| AutoMapperStartup |   286.346 μs | 5.0147 μs | 5.1498 μs |   281.524 μs |   284.592 μs |   299.321 μs | 0.275 |    2 | 5.8594 |      - | 104.11 KB |        1.11 |
| MapsterStartup    |     2.443 μs | 0.0230 μs | 0.0215 μs |     2.416 μs |     2.437 μs |     2.489 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.060 ms | 0.0097 ms | 0.0091 ms | 1.046 ms | 1.058 ms | 1.077 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  94.64 KB |        1.00 |
| AutoMapper | 3.211 ms | 0.0151 ms | 0.0134 ms | 3.195 ms | 3.210 ms | 3.236 ms |  3.03 |    0.03 |    3 | 15.6250 |  7.8125 | 310.27 KB |        3.28 |
| Mapster    | 2.473 ms | 0.0099 ms | 0.0087 ms | 2.454 ms | 2.475 ms | 2.483 ms |  2.33 |    0.02 |    2 | 46.8750 | 15.6250 | 768.74 KB |        8.12 |

---

*Benchmarks run automatically on every push to `main` with .NET 10. [See workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)*

<!-- BENCHMARK_RESULTS_END -->

## Features

<!-- FEATURES_START -->
- ✅ Compiled expression tree delegates (zero runtime reflection)
- ✅ `ForMember` / `MapFrom` custom mappings
- ✅ `Ignore()` members
- ✅ `ReverseMap()` bidirectional mapping
- ✅ Nested object mapping (inlined into parent expression tree)
- ✅ Collection mapping (`List<T>`, arrays, `HashSet<T>`, etc.)
- ✅ Flattening (`src.Address.Street` → `dest.AddressStreet`)
- ✅ Constructor mapping (auto-detects best-matching constructor for records)
- ✅ Profile-based configuration
- ✅ Assembly scanning
- ✅ Before/After map hooks
- ✅ Conditional mapping
- ✅ Null substitution
- ✅ `MaxDepth` for self-referencing types
- ✅ Inheritance mapping
- ✅ Enum mapping (int ↔ enum and string ↔ enum auto-conversion)
- ✅ `ForPath` for nested destination properties
- ✅ .NET Dependency Injection integration (transient `IMapper`, scoped service support)
- ✅ EF Core proxy / derived type resolution (base-type + interface walk)
- ✅ Constructor-based type conversion in `MapFrom(s => s)` patterns
- ✅ Configuration validation
- ✅ `CreateMap(Type, Type)` runtime type mapping
- ✅ `ITypeConverter<S,D>` / `ConvertUsing` custom converters
- ✅ `ShouldMapProperty` global property filter
- ✅ Patch / partial mapping via `mapper.Patch<S,D>(src, dest)`
- ✅ Inline validation rules via `.Validate()` (collects all failures before throwing)
- ✅ IQueryable projection via `ProjectTo<S,D>(config)` for EF Core / LINQ providers
<!-- FEATURES_END -->

## Mapping Tiers

EggMapper supports three complementary mapping approaches. Choose based on your use case:

| | **Runtime** (`EggMapper`) | **Tier 2** (`EggMapper.Generator`) | **Tier 3** (`EggMapper.ClassMapper`) |
|---|---|---|---|
| **API** | `MapperConfiguration` + `CreateMap` | `[MapTo(typeof(Dest))]` attribute | `[EggMapper]` partial class |
| **Mapping errors detected** | Runtime | ✅ Build time | ✅ Build time |
| **Reflection at map time** | None (expression trees) | ✅ None (generated code) | ✅ None (generated code) |
| **Startup cost** | Compilation (once) | ✅ None | ✅ None |
| **Custom logic** | Full (`ForMember`, hooks, etc.) | `AfterMap` hook | Full custom methods |
| **Reverse mapping** | `ReverseMap()` | Separate `[MapTo]` annotation | Declare both `partial` methods |
| **DI-friendly instance** | `IMapper` | N/A (extension methods) | ✅ `Instance` + constructors |
| **Migration from AutoMapper** | ✅ Drop-in | Via EGG1003 suggestion | New API |
| **Best for** | Complex/conditional mapping | Simple 1:1 copies | Custom logic + compile safety |

See [Migration Guide](docs/Migration-Guide.md) to move from runtime to compile-time APIs.

---

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](https://github.com/eggspot/EggMapper/wiki/Getting-Started) | Installation and your first runtime mapping |
| [Tier 2 Getting Started](docs/Tier2-Getting-Started.md) | Compile-time extension methods with `[MapTo]` |
| [Tier 3 Getting Started](docs/Tier3-Getting-Started.md) | Compile-time partial mapper classes with `[EggMapper]` |
| [Migration Guide](docs/Migration-Guide.md) | Moving from runtime to compile-time APIs |
| [Diagnostic Reference](docs/diagnostics/) | All EGG diagnostic codes explained |
| [Configuration](https://github.com/eggspot/EggMapper/wiki/Configuration) | `MapperConfiguration` options |
| [Profiles](https://github.com/eggspot/EggMapper/wiki/Profiles) | Organising maps with `Profile` |
| [Dependency Injection](https://github.com/eggspot/EggMapper/wiki/Dependency-Injection) | ASP.NET Core / DI integration |
| [Advanced Features](https://github.com/eggspot/EggMapper/wiki/Advanced-Features) | `ForMember`, conditions, hooks, etc. |
| [Performance](https://github.com/eggspot/EggMapper/wiki/Performance) | Benchmark methodology & tips |
| [API Reference](https://github.com/eggspot/EggMapper/wiki/API-Reference) | Full public API surface |

## Sponsor

EggMapper is built and maintained by [Eggspot](https://eggspot.app). If this library saves you time or money, consider supporting its development:

<a href="https://github.com/sponsors/eggspot">
  <img src="https://img.shields.io/badge/Sponsor_EggMapper-❤️-ea4aaa?style=for-the-badge&logo=github" alt="Sponsor EggMapper" />
</a>

Sponsorships help fund:
- Continuous performance optimization and benchmarking
- New feature development
- Bug fixes and maintenance
- Documentation and community support

## Contributing

We welcome contributions from the community! Here's how you can help:

- **Report bugs** — [Open an issue](https://github.com/eggspot/EggMapper/issues/new?template=bug_report.md)
- **Request features** — [Start a discussion](https://github.com/eggspot/EggMapper/discussions/new?category=ideas)
- **Submit code** — Fork, branch, and [open a pull request](https://github.com/eggspot/EggMapper/pulls)
- **Improve docs** — Edit files in the `docs/` folder (auto-synced to the wiki)
- **Share benchmarks** — Run on your hardware and share results

### Development Setup

```bash
git clone https://github.com/eggspot/EggMapper.git
cd EggMapper
dotnet build --configuration Release
dotnet test --configuration Release
```

### Contribution Guidelines

1. **Fork** the repository and create a branch from `main`
2. **Write tests** for any new functionality
3. **Run all tests** — `dotnet test --configuration Release` must pass on all TFMs
4. **Run benchmarks** if changing core mapping code — `cd src/EggMapper.Benchmarks && dotnet run -c Release -f net10.0 -- --filter *`
5. **Open a PR** with a clear description of the change

All contributors are recognized in the GitHub Release notes automatically.

---

*Powered by [Eggspot](https://eggspot.app)*

# 🥚 EggMapper

> **The fastest .NET runtime object-to-object mapper** — forked from AutoMapper's last open-source release, rebuilt for maximum performance. Drop-in replacement with the same API, 1.5–5× faster.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

📖 **[Full documentation →](https://eggspot.github.io/EggMapper/)**

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
> ⏱ **Last updated:** 2026-05-01 16:17 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 17.34 ns | **29.11 ns (1.7×)** | 31.14 ns (1.8×) | 84.79 ns (4.9×) | 17.93 ns (1.0×) |
| **Flattening** | 21.26 ns | **32.22 ns (1.5×)** | 38.78 ns (1.8×) | 90.32 ns (4.2×) | 27.58 ns (1.3×) |
| **Deep (2 nested)** | 61.93 ns | **77.45 ns (1.2×)** | 77.46 ns (1.2×) | 127.70 ns (2.1×) | 58.48 ns (0.9×) |
| **Complex (nest+coll)** | 83.14 ns | **101.57 ns (1.2×)** | 102.01 ns (1.2×) | 163.70 ns (2.0×) | 78.72 ns (0.9×) |
| **Collection (100)** | 2.091 μs | **2.052 μs (1.0×)** | 2.097 μs (1.0×) | 2.735 μs (1.3×) | 2.174 μs (1.0×) |
| **Deep Coll (100)** | 6.230 μs | **6.925 μs (1.1×)** | 6.762 μs (1.1×) | 7.637 μs (1.2×) | 6.368 μs (1.0×) |
| **Large Coll (1000)** | 20.55 μs | **19.73 μs (1.0×)** | 20.31 μs (1.0×) | 24.85 μs (1.2×) | 21.02 μs (1.0×) |
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
| Manual               |  17.34 ns | 0.268 ns | 0.238 ns |  16.90 ns |  17.37 ns |  17.74 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  29.11 ns | 0.578 ns | 0.731 ns |  27.22 ns |  29.24 ns |  30.09 ns |  1.68 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  84.79 ns | 0.519 ns | 0.485 ns |  83.33 ns |  84.98 ns |  85.26 ns |  4.89 |    0.07 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  31.14 ns | 0.574 ns | 0.564 ns |  29.70 ns |  31.22 ns |  32.02 ns |  1.80 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  17.93 ns | 0.422 ns | 0.452 ns |  17.04 ns |  18.02 ns |  18.57 ns |  1.03 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 485.92 ns | 1.222 ns | 1.020 ns | 484.55 ns | 485.55 ns | 488.15 ns | 28.03 |    0.38 |    6 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  17.88 ns | 0.294 ns | 0.275 ns |  17.46 ns |  17.90 ns |  18.29 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  19.22 ns | 0.398 ns | 0.373 ns |  18.55 ns |  19.30 ns |  19.73 ns |  1.11 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  21.26 ns | 0.484 ns | 0.725 ns |  19.76 ns |  21.44 ns |  22.34 ns |  1.00 |    0.05 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  32.22 ns | 0.643 ns | 0.602 ns |  31.42 ns |  32.05 ns |  33.39 ns |  1.52 |    0.06 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  90.32 ns | 0.418 ns | 0.371 ns |  89.30 ns |  90.40 ns |  90.73 ns |  4.25 |    0.15 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  38.78 ns | 0.761 ns | 0.782 ns |  36.96 ns |  39.12 ns |  39.39 ns |  1.83 |    0.07 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  27.58 ns | 0.329 ns | 0.308 ns |  27.17 ns |  27.45 ns |  28.26 ns |  1.30 |    0.05 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 537.05 ns | 0.989 ns | 0.925 ns | 535.04 ns | 537.06 ns | 538.65 ns | 25.29 |    0.87 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  61.93 ns | 0.753 ns | 0.667 ns |  60.96 ns |  62.05 ns |  63.16 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  77.45 ns | 1.537 ns | 1.888 ns |  73.78 ns |  77.62 ns |  80.19 ns |  1.25 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 127.70 ns | 2.190 ns | 2.049 ns | 122.14 ns | 128.34 ns | 129.62 ns |  2.06 |    0.04 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  77.46 ns | 1.494 ns | 1.834 ns |  73.36 ns |  77.64 ns |  80.73 ns |  1.25 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  58.48 ns | 1.225 ns | 1.676 ns |  54.45 ns |  58.74 ns |  61.30 ns |  0.94 |    0.03 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 515.14 ns | 2.579 ns | 2.412 ns | 509.46 ns | 514.87 ns | 518.59 ns |  8.32 |    0.09 |    5 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  83.14 ns | 1.232 ns | 1.152 ns |  81.25 ns |  83.10 ns |  84.96 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 101.57 ns | 2.022 ns | 2.408 ns |  96.47 ns | 101.87 ns | 105.43 ns |  1.22 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 163.70 ns | 1.075 ns | 0.898 ns | 162.07 ns | 163.61 ns | 165.88 ns |  1.97 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     | 102.01 ns | 1.561 ns | 1.460 ns |  98.77 ns | 102.68 ns | 103.51 ns |  1.23 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  78.72 ns | 1.632 ns | 2.726 ns |  72.60 ns |  79.34 ns |  82.07 ns |  0.95 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 582.89 ns | 1.955 ns | 1.829 ns | 578.15 ns | 583.05 ns | 585.63 ns |  7.01 |    0.10 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.091 μs | 0.0416 μs | 0.0717 μs | 1.911 μs | 2.091 μs | 2.230 μs |  1.00 |    0.05 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.052 μs | 0.0401 μs | 0.0601 μs | 1.847 μs | 2.062 μs | 2.147 μs |  0.98 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.735 μs | 0.0537 μs | 0.0527 μs | 2.582 μs | 2.748 μs | 2.789 μs |  1.31 |    0.05 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.097 μs | 0.0402 μs | 0.0395 μs | 2.038 μs | 2.091 μs | 2.183 μs |  1.00 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.174 μs | 0.0429 μs | 0.0573 μs | 2.033 μs | 2.180 μs | 2.253 μs |  1.04 |    0.05 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.797 μs | 0.0538 μs | 0.0528 μs | 2.717 μs | 2.784 μs | 2.901 μs |  1.34 |    0.05 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.230 μs | 0.1239 μs | 0.1475 μs | 5.864 μs | 6.215 μs | 6.468 μs |  1.00 |    0.03 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.925 μs | 0.0907 μs | 0.0848 μs | 6.782 μs | 6.938 μs | 7.070 μs |  1.11 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.637 μs | 0.1227 μs | 0.1148 μs | 7.291 μs | 7.640 μs | 7.761 μs |  1.23 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.762 μs | 0.1276 μs | 0.1365 μs | 6.505 μs | 6.766 μs | 7.009 μs |  1.09 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.368 μs | 0.1214 μs | 0.1247 μs | 6.162 μs | 6.361 μs | 6.581 μs |  1.02 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.993 μs | 0.0661 μs | 0.0618 μs | 5.864 μs | 6.007 μs | 6.104 μs |  0.96 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 20.55 μs | 0.382 μs | 0.357 μs | 20.06 μs | 20.45 μs | 21.08 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 19.73 μs | 0.392 μs | 0.801 μs | 18.07 μs | 19.84 μs | 21.39 μs |  0.96 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 24.85 μs | 0.349 μs | 0.309 μs | 24.40 μs | 24.84 μs | 25.37 μs |  1.21 |    0.02 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 20.31 μs | 0.404 μs | 0.466 μs | 19.57 μs | 20.31 μs | 21.12 μs |  0.99 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.02 μs | 0.411 μs | 0.687 μs | 18.90 μs | 21.12 μs | 22.32 μs |  1.02 |    0.04 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 23.63 μs | 0.469 μs | 0.502 μs | 22.90 μs | 23.62 μs | 24.60 μs |  1.15 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,206.029 μs | 15.1104 μs | 14.1343 μs | 1,179.457 μs | 1,202.764 μs | 1,236.585 μs | 1.000 |    0.02 |    3 | 3.9063 | 1.9531 |  95.06 KB |        1.00 |
| AutoMapperStartup |   290.950 μs |  1.4950 μs |  1.2484 μs |   288.953 μs |   291.367 μs |   292.884 μs | 0.241 |    0.00 |    2 | 5.8594 |      - |    104 KB |        1.09 |
| MapsterStartup    |     2.807 μs |  0.0560 μs |  0.0889 μs |     2.603 μs |     2.794 μs |     2.969 μs | 0.002 |    0.00 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.199 ms | 0.0191 ms | 0.0179 ms | 1.160 ms | 1.200 ms | 1.229 ms |  1.00 |    0.02 |    1 |  5.8594 |  1.9531 |  95.79 KB |        1.00 |
| AutoMapper | 3.397 ms | 0.0412 ms | 0.0385 ms | 3.303 ms | 3.396 ms | 3.445 ms |  2.83 |    0.05 |    3 | 15.6250 |  7.8125 | 310.28 KB |        3.24 |
| Mapster    | 2.591 ms | 0.0295 ms | 0.0275 ms | 2.552 ms | 2.584 ms | 2.657 ms |  2.16 |    0.04 |    2 | 39.0625 | 15.6250 | 757.37 KB |        7.91 |

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

| | **Runtime** (`EggMapper`) | **Attribute Mapper** (`EggMapper.Generator`) | **Class Mapper** (`EggMapper.ClassMapper`) |
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

See [Migration Guide](https://eggspot.github.io/EggMapper/Migration-Guide.html) to move from runtime to compile-time APIs.

---

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](https://eggspot.github.io/EggMapper/Getting-Started.html) | Installation and your first runtime mapping |
| [Attribute Mapper](https://eggspot.github.io/EggMapper/Attribute-Mapper.html) | Compile-time extension methods with `[MapTo]` |
| [Class Mapper](https://eggspot.github.io/EggMapper/Class-Mapper.html) | Compile-time partial mapper classes with `[EggMapper]` |
| [Migration Guide](https://eggspot.github.io/EggMapper/Migration-Guide.html) | Moving from AutoMapper or runtime to compile-time APIs |
| [Configuration](https://eggspot.github.io/EggMapper/Configuration.html) | `MapperConfiguration` options |
| [Profiles](https://eggspot.github.io/EggMapper/Profiles.html) | Organising maps with `Profile` |
| [Dependency Injection](https://eggspot.github.io/EggMapper/Dependency-Injection.html) | ASP.NET Core / DI integration |
| [Advanced Features](https://eggspot.github.io/EggMapper/Advanced-Features.html) | `ForMember`, conditions, hooks, etc. |
| [Performance](https://eggspot.github.io/EggMapper/Performance.html) | Benchmark methodology & tips |
| [API Reference](https://eggspot.github.io/EggMapper/API-Reference.html) | Full public API surface |
| [Diagnostic Reference](https://eggspot.github.io/EggMapper/diagnostics/EGG1002.html) | All EGG diagnostic codes explained |

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
- **Improve docs** — Edit files in the `docs/` folder (published to [eggspot.github.io/EggMapper](https://eggspot.github.io/EggMapper/))
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

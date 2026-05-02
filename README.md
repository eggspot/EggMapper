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
> ⏱ **Last updated:** 2026-05-02 03:51 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.91 ns | **26.49 ns (1.7×)** | 31.55 ns (2.0×) | 82.40 ns (5.2×) | 15.83 ns (1.0×) |
| **Flattening** | 20.81 ns | **29.23 ns (1.4×)** | 38.76 ns (1.9×) | 91.32 ns (4.4×) | 26.38 ns (1.3×) |
| **Deep (2 nested)** | 57.24 ns | **66.84 ns (1.2×)** | 73.03 ns (1.3×) | 128.61 ns (2.2×) | 53.07 ns (0.9×) |
| **Complex (nest+coll)** | 71.96 ns | **92.92 ns (1.3×)** | 91.27 ns (1.3×) | 157.04 ns (2.2×) | 72.08 ns (1.0×) |
| **Collection (100)** | 1.745 μs | **1.723 μs (1.0×)** | 1.720 μs (1.0×) | 2.331 μs (1.3×) | 1.857 μs (1.1×) |
| **Deep Coll (100)** | 5.346 μs | **5.832 μs (1.1×)** | 6.134 μs (1.1×) | 6.971 μs (1.3×) | 5.645 μs (1.1×) |
| **Large Coll (1000)** | 19.88 μs | **19.73 μs (1.0×)** | 18.98 μs (0.9×) | 23.70 μs (1.2×) | 20.26 μs (1.0×) |
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
| Manual               |  15.91 ns | 0.378 ns | 0.354 ns |  15.15 ns |  15.88 ns |  16.40 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.49 ns | 0.363 ns | 0.340 ns |  25.94 ns |  26.38 ns |  27.00 ns |  1.67 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  82.40 ns | 0.449 ns | 0.398 ns |  81.46 ns |  82.45 ns |  82.99 ns |  5.18 |    0.12 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  31.55 ns | 0.334 ns | 0.312 ns |  30.91 ns |  31.55 ns |  32.03 ns |  1.98 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.83 ns | 0.306 ns | 0.286 ns |  15.09 ns |  15.87 ns |  16.16 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 510.18 ns | 1.493 ns | 1.397 ns | 508.19 ns | 509.91 ns | 512.91 ns | 32.08 |    0.70 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.59 ns | 0.195 ns | 0.163 ns |  15.35 ns |  15.61 ns |  15.80 ns |  0.98 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.20 ns | 0.260 ns | 0.230 ns |  15.77 ns |  16.21 ns |  16.63 ns |  1.02 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.81 ns | 0.459 ns | 0.451 ns |  20.20 ns |  20.76 ns |  21.58 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.23 ns | 0.159 ns | 0.141 ns |  29.09 ns |  29.19 ns |  29.52 ns |  1.41 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  91.32 ns | 0.512 ns | 0.479 ns |  90.21 ns |  91.37 ns |  91.91 ns |  4.39 |    0.09 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  38.76 ns | 0.178 ns | 0.166 ns |  38.39 ns |  38.75 ns |  39.02 ns |  1.86 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  26.38 ns | 0.375 ns | 0.351 ns |  25.85 ns |  26.45 ns |  26.97 ns |  1.27 |    0.03 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 534.66 ns | 1.542 ns | 1.443 ns | 530.07 ns | 534.80 ns | 536.19 ns | 25.71 |    0.54 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  57.24 ns | 1.205 ns | 1.127 ns |  55.31 ns |  57.20 ns |  59.37 ns |  1.00 |    0.03 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  66.84 ns | 1.294 ns | 1.636 ns |  63.27 ns |  66.84 ns |  69.49 ns |  1.17 |    0.04 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 128.61 ns | 1.567 ns | 1.466 ns | 126.09 ns | 128.53 ns | 131.33 ns |  2.25 |    0.05 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  73.03 ns | 0.668 ns | 0.522 ns |  71.68 ns |  73.22 ns |  73.52 ns |  1.28 |    0.03 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  53.07 ns | 1.030 ns | 0.964 ns |  51.28 ns |  52.82 ns |  54.60 ns |  0.93 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 504.22 ns | 3.764 ns | 3.521 ns | 498.42 ns | 503.28 ns | 511.99 ns |  8.81 |    0.18 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  71.96 ns | 1.284 ns | 1.201 ns |  70.52 ns |  71.75 ns |  75.11 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  92.92 ns | 1.414 ns | 1.323 ns |  90.67 ns |  92.67 ns |  96.02 ns |  1.29 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 157.04 ns | 1.712 ns | 1.601 ns | 152.92 ns | 157.35 ns | 159.26 ns |  2.18 |    0.04 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  91.27 ns | 1.466 ns | 1.371 ns |  89.44 ns |  91.14 ns |  94.45 ns |  1.27 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  72.08 ns | 0.714 ns | 0.668 ns |  70.98 ns |  72.15 ns |  73.50 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 568.68 ns | 1.595 ns | 1.414 ns | 566.56 ns | 568.69 ns | 571.48 ns |  7.90 |    0.13 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.745 μs | 0.0148 μs | 0.0124 μs | 1.723 μs | 1.746 μs | 1.763 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.723 μs | 0.0173 μs | 0.0162 μs | 1.683 μs | 1.728 μs | 1.745 μs |  0.99 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.331 μs | 0.0224 μs | 0.0210 μs | 2.294 μs | 2.329 μs | 2.372 μs |  1.34 |    0.01 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.720 μs | 0.0197 μs | 0.0175 μs | 1.690 μs | 1.717 μs | 1.760 μs |  0.99 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.857 μs | 0.0180 μs | 0.0150 μs | 1.831 μs | 1.855 μs | 1.890 μs |  1.06 |    0.01 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.494 μs | 0.0235 μs | 0.0220 μs | 2.446 μs | 2.499 μs | 2.522 μs |  1.43 |    0.02 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.346 μs | 0.0658 μs | 0.0615 μs | 5.287 μs | 5.327 μs | 5.494 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.832 μs | 0.1154 μs | 0.2223 μs | 5.555 μs | 5.809 μs | 6.345 μs |  1.09 |    0.04 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.971 μs | 0.1352 μs | 0.1447 μs | 6.760 μs | 6.939 μs | 7.271 μs |  1.30 |    0.03 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.134 μs | 0.0747 μs | 0.0624 μs | 6.048 μs | 6.140 μs | 6.234 μs |  1.15 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.645 μs | 0.0848 μs | 0.0752 μs | 5.557 μs | 5.624 μs | 5.794 μs |  1.06 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.450 μs | 0.0759 μs | 0.0710 μs | 5.287 μs | 5.452 μs | 5.550 μs |  1.02 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.88 μs | 0.239 μs | 0.223 μs | 19.51 μs | 19.84 μs | 20.27 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 19.73 μs | 0.301 μs | 0.267 μs | 19.20 μs | 19.76 μs | 20.21 μs |  0.99 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 23.70 μs | 0.466 μs | 0.606 μs | 21.98 μs | 23.87 μs | 24.58 μs |  1.19 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.98 μs | 0.371 μs | 0.629 μs | 17.53 μs | 18.98 μs | 20.22 μs |  0.95 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 20.26 μs | 0.368 μs | 0.326 μs | 19.60 μs | 20.38 μs | 20.66 μs |  1.02 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 22.89 μs | 0.452 μs | 0.521 μs | 22.00 μs | 22.96 μs | 23.89 μs |  1.15 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,171.507 μs | 8.7389 μs | 7.7468 μs | 1,159.259 μs | 1,172.628 μs | 1,187.665 μs | 1.000 |    3 | 5.8594 | 3.9063 |  95.92 KB |        1.00 |
| AutoMapperStartup |   283.511 μs | 1.2598 μs | 0.9836 μs |   282.436 μs |   283.294 μs |   285.811 μs | 0.242 |    2 | 5.8594 |      - | 104.09 KB |        1.09 |
| MapsterStartup    |     2.751 μs | 0.0546 μs | 0.0670 μs |     2.634 μs |     2.751 μs |     2.896 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.167 ms | 0.0076 ms | 0.0068 ms | 1.158 ms | 1.166 ms | 1.182 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.12 KB |        1.00 |
| AutoMapper | 3.334 ms | 0.0521 ms | 0.0462 ms | 3.229 ms | 3.335 ms | 3.404 ms |  2.86 |    0.04 |    3 | 15.6250 |  7.8125 |  310.3 KB |        3.23 |
| Mapster    | 2.513 ms | 0.0191 ms | 0.0179 ms | 2.487 ms | 2.514 ms | 2.547 ms |  2.15 |    0.02 |    2 | 39.0625 | 15.6250 | 757.44 KB |        7.88 |

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

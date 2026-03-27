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
> ⏱ **Last updated:** 2026-03-27 00:18 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 14.61 ns | **26.80 ns (1.8×)** | 27.77 ns (1.9×) | 82.54 ns (5.7×) | 14.61 ns (1.0×) |
| **Flattening** | 18.33 ns | **30.46 ns (1.7×)** | 36.05 ns (2.0×) | 86.14 ns (4.7×) | 27.71 ns (1.5×) |
| **Deep (2 nested)** | 52.45 ns | **63.03 ns (1.2×)** | 67.55 ns (1.3×) | 119.09 ns (2.3×) | 47.94 ns (0.9×) |
| **Complex (nest+coll)** | 94.87 ns | **88.12 ns (0.9×)** | 86.78 ns (0.9×) | 147.83 ns (1.6×) | 67.92 ns (0.7×) |
| **Collection (100)** | 1.668 μs | **1.667 μs (1.0×)** | 1.654 μs (1.0×) | 2.265 μs (1.4×) | 1.779 μs (1.1×) |
| **Deep Coll (100)** | 5.061 μs | **5.471 μs (1.1×)** | 5.649 μs (1.1×) | 6.219 μs (1.2×) | 5.148 μs (1.0×) |
| **Large Coll (1000)** | 21.95 μs | **18.54 μs (0.8×)** | 16.48 μs (0.8×) | 20.30 μs (0.9×) | 17.93 μs (0.8×) |
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
| Manual               |  14.61 ns | 0.091 ns | 0.081 ns |  14.52 ns |  14.58 ns |  14.76 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.80 ns | 0.526 ns | 0.962 ns |  25.42 ns |  26.44 ns |  29.15 ns |  1.83 |    0.07 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  82.54 ns | 0.234 ns | 0.195 ns |  82.39 ns |  82.47 ns |  83.02 ns |  5.65 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.77 ns | 0.129 ns | 0.108 ns |  27.63 ns |  27.72 ns |  27.98 ns |  1.90 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  14.61 ns | 0.094 ns | 0.083 ns |  14.52 ns |  14.57 ns |  14.77 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 470.57 ns | 0.267 ns | 0.223 ns | 470.09 ns | 470.62 ns | 470.90 ns | 32.21 |    0.17 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  14.60 ns | 0.044 ns | 0.042 ns |  14.54 ns |  14.57 ns |  14.67 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.12 ns | 0.040 ns | 0.037 ns |  15.07 ns |  15.11 ns |  15.22 ns |  1.04 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.33 ns | 0.049 ns | 0.046 ns |  18.27 ns |  18.32 ns |  18.45 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.46 ns | 0.564 ns | 0.528 ns |  29.47 ns |  30.73 ns |  31.12 ns |  1.66 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  86.14 ns | 0.071 ns | 0.059 ns |  86.03 ns |  86.16 ns |  86.23 ns |  4.70 |    0.01 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  36.05 ns | 0.143 ns | 0.127 ns |  35.82 ns |  36.05 ns |  36.26 ns |  1.97 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  27.71 ns | 0.718 ns | 2.116 ns |  22.90 ns |  28.22 ns |  30.45 ns |  1.51 |    0.11 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 535.84 ns | 1.687 ns | 1.578 ns | 533.32 ns | 536.24 ns | 538.05 ns | 29.23 |    0.11 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Median    | Min       | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  52.45 ns | 1.024 ns | 2.045 ns |  51.60 ns |  50.64 ns |  58.52 ns |  1.00 |    0.05 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  63.03 ns | 0.583 ns | 0.517 ns |  62.83 ns |  62.41 ns |  64.16 ns |  1.20 |    0.05 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 119.09 ns | 0.336 ns | 0.298 ns | 119.03 ns | 118.72 ns | 119.67 ns |  2.27 |    0.08 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  67.55 ns | 0.590 ns | 0.493 ns |  67.39 ns |  66.86 ns |  68.68 ns |  1.29 |    0.05 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  47.94 ns | 0.174 ns | 0.145 ns |  47.92 ns |  47.76 ns |  48.29 ns |  0.92 |    0.03 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 508.10 ns | 1.311 ns | 1.162 ns | 507.56 ns | 506.66 ns | 510.56 ns |  9.70 |    0.36 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|-----:|-------:|----------:|------------:|
| Manual      |  94.87 ns | 0.217 ns | 0.169 ns |  94.60 ns |  94.89 ns |  95.15 ns |  1.00 |    3 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  88.12 ns | 0.514 ns | 0.481 ns |  87.44 ns |  88.09 ns |  88.99 ns |  0.93 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 147.83 ns | 0.441 ns | 0.391 ns | 147.38 ns | 147.68 ns | 148.82 ns |  1.56 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  86.78 ns | 0.294 ns | 0.261 ns |  86.23 ns |  86.81 ns |  87.26 ns |  0.91 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  67.92 ns | 0.280 ns | 0.249 ns |  67.47 ns |  67.89 ns |  68.33 ns |  0.72 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 549.21 ns | 0.485 ns | 0.379 ns | 548.61 ns | 549.24 ns | 549.83 ns |  5.79 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.668 μs | 0.0045 μs | 0.0040 μs | 1.660 μs | 1.670 μs | 1.673 μs |  1.00 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.667 μs | 0.0158 μs | 0.0140 μs | 1.642 μs | 1.668 μs | 1.686 μs |  1.00 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.265 μs | 0.0130 μs | 0.0122 μs | 2.246 μs | 2.264 μs | 2.287 μs |  1.36 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.654 μs | 0.0114 μs | 0.0101 μs | 1.636 μs | 1.654 μs | 1.677 μs |  0.99 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.779 μs | 0.0078 μs | 0.0073 μs | 1.769 μs | 1.778 μs | 1.794 μs |  1.07 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.469 μs | 0.0101 μs | 0.0090 μs | 2.460 μs | 2.467 μs | 2.487 μs |  1.48 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.061 μs | 0.0308 μs | 0.0288 μs | 5.011 μs | 5.069 μs | 5.102 μs |  1.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.471 μs | 0.0284 μs | 0.0266 μs | 5.421 μs | 5.477 μs | 5.506 μs |  1.08 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.219 μs | 0.0137 μs | 0.0121 μs | 6.198 μs | 6.218 μs | 6.239 μs |  1.23 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.649 μs | 0.0507 μs | 0.0449 μs | 5.558 μs | 5.662 μs | 5.716 μs |  1.12 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.148 μs | 0.0424 μs | 0.0376 μs | 5.109 μs | 5.135 μs | 5.228 μs |  1.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.970 μs | 0.0191 μs | 0.0170 μs | 4.945 μs | 4.963 μs | 5.004 μs |  0.98 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 21.95 μs | 0.433 μs | 0.635 μs | 20.81 μs | 21.95 μs | 23.10 μs |  1.00 |    0.04 |    3 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.54 μs | 0.556 μs | 1.638 μs | 16.24 μs | 18.55 μs | 21.85 μs |  0.85 |    0.08 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.30 μs | 0.071 μs | 0.059 μs | 20.18 μs | 20.31 μs | 20.41 μs |  0.93 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 16.48 μs | 0.060 μs | 0.050 μs | 16.34 μs | 16.49 μs | 16.54 μs |  0.75 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.93 μs | 0.069 μs | 0.065 μs | 17.84 μs | 17.92 μs | 18.06 μs |  0.82 |    0.02 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.94 μs | 0.068 μs | 0.060 μs | 19.82 μs | 19.96 μs | 20.02 μs |  0.91 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,099.390 μs | 5.9484 μs | 5.5642 μs | 1,091.132 μs | 1,098.482 μs | 1,111.240 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.14 KB |        1.00 |
| AutoMapperStartup |   274.826 μs | 0.9211 μs | 0.8616 μs |   273.514 μs |   274.634 μs |   276.433 μs | 0.250 |    2 | 5.8594 |      - | 103.97 KB |        1.09 |
| MapsterStartup    |     2.410 μs | 0.0068 μs | 0.0060 μs |     2.403 μs |     2.408 μs |     2.424 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.132 ms | 0.0137 ms | 0.0121 ms | 1.105 ms | 1.131 ms | 1.153 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  95.61 KB |        1.00 |
| AutoMapper | 3.149 ms | 0.0064 ms | 0.0050 ms | 3.140 ms | 3.149 ms | 3.159 ms |  2.78 |    0.03 |    3 | 15.6250 |  7.8125 | 310.08 KB |        3.24 |
| Mapster    | 2.412 ms | 0.0169 ms | 0.0158 ms | 2.395 ms | 2.404 ms | 2.438 ms |  2.13 |    0.03 |    2 | 39.0625 | 15.6250 | 759.48 KB |        7.94 |

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

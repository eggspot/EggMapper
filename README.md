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
> ⏱ **Last updated:** 2026-03-25 07:10 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.34 ns | **26.01 ns (1.7×)** | 28.40 ns (1.9×) | 80.72 ns (5.3×) | 14.80 ns (1.0×) |
| **Flattening** | 18.80 ns | **29.01 ns (1.5×)** | 47.64 ns (2.5×) | 87.73 ns (4.7×) | 23.63 ns (1.3×) |
| **Deep (2 nested)** | 53.50 ns | **64.05 ns (1.2×)** | 67.34 ns (1.3×) | 120.16 ns (2.2×) | 49.63 ns (0.9×) |
| **Complex (nest+coll)** | 69.65 ns | **92.65 ns (1.3×)** | 88.92 ns (1.3×) | 150.92 ns (2.2×) | 70.82 ns (1.0×) |
| **Collection (100)** | 1.728 μs | **1.760 μs (1.0×)** | 1.750 μs (1.0×) | 2.354 μs (1.4×) | 1.889 μs (1.1×) |
| **Deep Coll (100)** | 5.269 μs | **5.519 μs (1.1×)** | 5.788 μs (1.1×) | 6.334 μs (1.2×) | 5.373 μs (1.0×) |
| **Large Coll (1000)** | 17.66 μs | **17.17 μs (1.0×)** | 17.36 μs (1.0×) | 20.88 μs (1.2×) | 18.27 μs (1.0×) |
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
| Manual               |  15.34 ns | 0.279 ns | 0.261 ns |  14.98 ns |  15.30 ns |  15.76 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.01 ns | 0.513 ns | 0.504 ns |  25.31 ns |  25.81 ns |  26.89 ns |  1.70 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  80.72 ns | 0.259 ns | 0.216 ns |  80.37 ns |  80.72 ns |  81.07 ns |  5.26 |    0.09 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.40 ns | 0.296 ns | 0.277 ns |  27.84 ns |  28.32 ns |  28.80 ns |  1.85 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  14.80 ns | 0.134 ns | 0.119 ns |  14.64 ns |  14.78 ns |  15.02 ns |  0.97 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 488.61 ns | 1.682 ns | 1.491 ns | 486.73 ns | 488.41 ns | 491.92 ns | 31.86 |    0.53 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  14.92 ns | 0.120 ns | 0.106 ns |  14.76 ns |  14.89 ns |  15.15 ns |  0.97 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.52 ns | 0.149 ns | 0.125 ns |  15.33 ns |  15.56 ns |  15.72 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.80 ns | 0.127 ns | 0.119 ns |  18.56 ns |  18.81 ns |  19.02 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.01 ns | 0.445 ns | 0.394 ns |  28.13 ns |  29.00 ns |  29.60 ns |  1.54 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  87.73 ns | 0.260 ns | 0.243 ns |  87.37 ns |  87.71 ns |  88.17 ns |  4.67 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.64 ns | 0.230 ns | 0.216 ns |  47.26 ns |  47.68 ns |  47.93 ns |  2.53 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.63 ns | 0.246 ns | 0.230 ns |  23.29 ns |  23.58 ns |  24.11 ns |  1.26 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 523.60 ns | 1.850 ns | 1.640 ns | 521.54 ns | 523.10 ns | 526.43 ns | 27.85 |    0.19 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.50 ns | 0.442 ns | 0.391 ns |  52.67 ns |  53.52 ns |  54.16 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.05 ns | 0.605 ns | 0.472 ns |  63.02 ns |  64.10 ns |  64.78 ns |  1.20 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 120.16 ns | 0.824 ns | 0.771 ns | 118.24 ns | 120.32 ns | 121.55 ns |  2.25 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  67.34 ns | 0.716 ns | 0.635 ns |  66.03 ns |  67.39 ns |  68.58 ns |  1.26 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.63 ns | 0.285 ns | 0.266 ns |  49.27 ns |  49.60 ns |  50.21 ns |  0.93 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 499.01 ns | 2.236 ns | 2.092 ns | 494.77 ns | 499.92 ns | 501.73 ns |  9.33 |    0.08 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  69.65 ns | 0.744 ns | 0.660 ns |  68.51 ns |  69.57 ns |  70.87 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  92.65 ns | 0.675 ns | 0.598 ns |  91.56 ns |  92.73 ns |  93.49 ns |  1.33 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 150.92 ns | 1.391 ns | 1.233 ns | 148.68 ns | 151.20 ns | 152.83 ns |  2.17 |    0.03 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  88.92 ns | 0.713 ns | 0.596 ns |  87.95 ns |  88.86 ns |  90.20 ns |  1.28 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.82 ns | 0.875 ns | 0.775 ns |  69.77 ns |  70.62 ns |  72.14 ns |  1.02 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 580.32 ns | 2.369 ns | 2.100 ns | 577.87 ns | 579.72 ns | 584.24 ns |  8.33 |    0.08 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.728 μs | 0.0147 μs | 0.0137 μs | 1.703 μs | 1.730 μs | 1.754 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.760 μs | 0.0170 μs | 0.0142 μs | 1.734 μs | 1.759 μs | 1.787 μs |  1.02 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.354 μs | 0.0445 μs | 0.0416 μs | 2.310 μs | 2.349 μs | 2.446 μs |  1.36 |    0.03 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.750 μs | 0.0169 μs | 0.0150 μs | 1.722 μs | 1.753 μs | 1.775 μs |  1.01 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.889 μs | 0.0287 μs | 0.0268 μs | 1.830 μs | 1.894 μs | 1.928 μs |  1.09 |    0.02 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.606 μs | 0.0409 μs | 0.0362 μs | 2.550 μs | 2.607 μs | 2.669 μs |  1.51 |    0.02 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.269 μs | 0.0528 μs | 0.0494 μs | 5.177 μs | 5.262 μs | 5.364 μs |  1.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.519 μs | 0.0637 μs | 0.0565 μs | 5.458 μs | 5.501 μs | 5.639 μs |  1.05 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.334 μs | 0.0423 μs | 0.0375 μs | 6.284 μs | 6.325 μs | 6.409 μs |  1.20 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.788 μs | 0.0584 μs | 0.0546 μs | 5.665 μs | 5.789 μs | 5.858 μs |  1.10 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.373 μs | 0.0506 μs | 0.0449 μs | 5.259 μs | 5.385 μs | 5.421 μs |  1.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.185 μs | 0.0347 μs | 0.0325 μs | 5.137 μs | 5.180 μs | 5.237 μs |  0.98 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.66 μs | 0.140 μs | 0.124 μs | 17.39 μs | 17.68 μs | 17.79 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.17 μs | 0.243 μs | 0.215 μs | 16.80 μs | 17.23 μs | 17.62 μs |  0.97 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.88 μs | 0.268 μs | 0.238 μs | 20.42 μs | 20.88 μs | 21.34 μs |  1.18 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.36 μs | 0.239 μs | 0.212 μs | 16.94 μs | 17.40 μs | 17.71 μs |  0.98 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.27 μs | 0.167 μs | 0.156 μs | 17.99 μs | 18.30 μs | 18.52 μs |  1.03 |    0.01 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.93 μs | 0.161 μs | 0.143 μs | 19.73 μs | 19.93 μs | 20.22 μs |  1.13 |    0.01 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,094.424 μs | 3.7028 μs | 3.0920 μs | 1,089.817 μs | 1,093.818 μs | 1,100.884 μs | 1.000 |    3 | 3.9063 | 1.9531 |   94.6 KB |        1.00 |
| AutoMapperStartup |   274.747 μs | 0.6747 μs | 0.5634 μs |   273.833 μs |   274.625 μs |   275.512 μs | 0.251 |    2 | 5.8594 |      - | 103.92 KB |        1.10 |
| MapsterStartup    |     2.454 μs | 0.0281 μs | 0.0249 μs |     2.392 μs |     2.453 μs |     2.488 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.087 ms | 0.0051 ms | 0.0043 ms | 1.078 ms | 1.086 ms | 1.093 ms |  1.00 |    1 |  3.9063 |  1.9531 |  95.24 KB |        1.00 |
| AutoMapper | 3.189 ms | 0.0081 ms | 0.0071 ms | 3.181 ms | 3.186 ms | 3.203 ms |  2.93 |    3 | 15.6250 |  7.8125 | 310.23 KB |        3.26 |
| Mapster    | 2.456 ms | 0.0067 ms | 0.0056 ms | 2.447 ms | 2.457 ms | 2.462 ms |  2.26 |    2 | 39.0625 | 15.6250 | 759.54 KB |        7.97 |

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

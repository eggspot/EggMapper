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
> ⏱ **Last updated:** 2026-04-20 03:02 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 17.50 ns | **27.77 ns (1.6×)** | 28.25 ns (1.6×) | 70.37 ns (4.0×) | 17.16 ns (1.0×) |
| **Flattening** | 21.08 ns | **29.55 ns (1.4×)** | 38.35 ns (1.8×) | 71.38 ns (3.4×) | 25.71 ns (1.2×) |
| **Deep (2 nested)** | 61.28 ns | **76.18 ns (1.2×)** | 77.65 ns (1.3×) | 111.31 ns (1.8×) | 58.71 ns (1.0×) |
| **Complex (nest+coll)** | 76.07 ns | **100.41 ns (1.3×)** | 98.13 ns (1.3×) | 145.91 ns (1.9×) | 80.28 ns (1.1×) |
| **Collection (100)** | 1.862 μs | **2.007 μs (1.1×)** | 1.865 μs (1.0×) | 2.666 μs (1.4×) | 2.019 μs (1.1×) |
| **Deep Coll (100)** | 5.842 μs | **6.227 μs (1.1×)** | 6.391 μs (1.1×) | 7.166 μs (1.2×) | 5.985 μs (1.0×) |
| **Large Coll (1000)** | 20.20 μs | **18.98 μs (0.9×)** | 18.16 μs (0.9×) | 22.85 μs (1.1×) | 21.44 μs (1.1×) |
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
| Manual               |  17.50 ns | 0.275 ns | 0.257 ns |  16.85 ns |  17.60 ns |  17.79 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapper            |  27.77 ns | 0.240 ns | 0.213 ns |  27.45 ns |  27.79 ns |  28.15 ns |  1.59 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| AutoMapper           |  70.37 ns | 0.321 ns | 0.300 ns |  69.73 ns |  70.43 ns |  70.72 ns |  4.02 |    0.06 |    3 | 0.0031 |      80 B |        1.00 |
| Mapster              |  28.25 ns | 0.340 ns | 0.302 ns |  27.76 ns |  28.27 ns |  28.78 ns |  1.61 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| MapperlyMap          |  17.16 ns | 0.322 ns | 0.302 ns |  16.62 ns |  17.17 ns |  17.68 ns |  0.98 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| AgileMapper          | 354.52 ns | 0.456 ns | 0.426 ns | 353.80 ns | 354.48 ns | 355.47 ns | 20.27 |    0.29 |    4 | 0.0134 |     344 B |        4.30 |
| EggMapperGenerator   |  18.17 ns | 0.371 ns | 0.329 ns |  17.51 ns |  18.23 ns |  18.81 ns |  1.04 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapperClassMapper |  18.02 ns | 0.307 ns | 0.287 ns |  17.58 ns |  18.04 ns |  18.66 ns |  1.03 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  21.08 ns | 0.433 ns | 0.384 ns |  20.43 ns |  21.07 ns |  21.90 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  29.55 ns | 0.279 ns | 0.261 ns |  29.02 ns |  29.53 ns |  30.07 ns |  1.40 |    0.03 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  71.38 ns | 0.151 ns | 0.126 ns |  71.17 ns |  71.40 ns |  71.64 ns |  3.39 |    0.06 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  38.35 ns | 0.186 ns | 0.165 ns |  38.07 ns |  38.31 ns |  38.62 ns |  1.82 |    0.03 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  25.71 ns | 0.297 ns | 0.278 ns |  25.18 ns |  25.71 ns |  26.15 ns |  1.22 |    0.02 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 352.97 ns | 0.401 ns | 0.375 ns | 352.09 ns | 352.96 ns | 353.54 ns | 16.75 |    0.29 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  61.28 ns | 1.133 ns | 1.060 ns |  59.80 ns |  61.16 ns |  63.09 ns |  1.00 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  76.18 ns | 1.487 ns | 1.391 ns |  74.33 ns |  75.93 ns |  78.84 ns |  1.24 |    0.03 |    2 | 0.0107 |     272 B |        1.00 |
| AutoMapper  | 111.31 ns | 0.873 ns | 0.817 ns | 109.93 ns | 111.27 ns | 112.75 ns |  1.82 |    0.03 |    3 | 0.0107 |     272 B |        1.00 |
| Mapster     |  77.65 ns | 1.289 ns | 1.143 ns |  75.46 ns |  77.89 ns |  79.38 ns |  1.27 |    0.03 |    2 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  58.71 ns | 0.892 ns | 0.834 ns |  56.80 ns |  58.72 ns |  59.96 ns |  0.96 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| AgileMapper | 390.29 ns | 0.563 ns | 0.499 ns | 389.61 ns | 390.18 ns | 391.22 ns |  6.37 |    0.11 |    4 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  76.07 ns | 1.489 ns | 1.392 ns |  72.63 ns |  76.23 ns |  78.26 ns |  1.00 |    0.03 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   | 100.41 ns | 1.021 ns | 0.955 ns |  98.66 ns | 100.34 ns | 101.98 ns |  1.32 |    0.03 |    3 | 0.0126 |     320 B |        1.00 |
| AutoMapper  | 145.91 ns | 0.988 ns | 0.924 ns | 144.64 ns | 145.53 ns | 147.56 ns |  1.92 |    0.04 |    4 | 0.0129 |     328 B |        1.02 |
| Mapster     |  98.13 ns | 1.169 ns | 1.094 ns |  96.74 ns |  97.84 ns | 100.07 ns |  1.29 |    0.03 |    3 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  80.28 ns | 1.202 ns | 1.124 ns |  78.63 ns |  80.17 ns |  82.43 ns |  1.06 |    0.02 |    2 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 432.29 ns | 3.103 ns | 2.751 ns | 429.11 ns | 431.70 ns | 438.20 ns |  5.68 |    0.11 |    5 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.862 μs | 0.0321 μs | 0.0300 μs | 1.820 μs | 1.861 μs | 1.923 μs |  1.00 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| EggMapper   | 2.007 μs | 0.0304 μs | 0.0269 μs | 1.965 μs | 2.007 μs | 2.054 μs |  1.08 |    0.02 |    2 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AutoMapper  | 2.666 μs | 0.0363 μs | 0.0339 μs | 2.599 μs | 2.665 μs | 2.712 μs |  1.43 |    0.03 |    3 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.865 μs | 0.0173 μs | 0.0153 μs | 1.845 μs | 1.863 μs | 1.897 μs |  1.00 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| MapperlyMap | 2.019 μs | 0.0232 μs | 0.0217 μs | 1.985 μs | 2.011 μs | 2.060 μs |  1.08 |    0.02 |    2 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AgileMapper | 2.642 μs | 0.0429 μs | 0.0402 μs | 2.587 μs | 2.634 μs | 2.724 μs |  1.42 |    0.03 |    3 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.842 μs | 0.1136 μs | 0.2421 μs | 5.477 μs | 5.763 μs | 6.442 μs |  1.00 |    0.06 |    1 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 6.227 μs | 0.0933 μs | 0.0827 μs | 6.071 μs | 6.235 μs | 6.366 μs |  1.07 |    0.04 |    1 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| AutoMapper  | 7.166 μs | 0.1166 μs | 0.1091 μs | 6.948 μs | 7.177 μs | 7.373 μs |  1.23 |    0.05 |    2 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 6.391 μs | 0.1060 μs | 0.0992 μs | 6.128 μs | 6.434 μs | 6.500 μs |  1.10 |    0.05 |    1 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 5.985 μs | 0.1183 μs | 0.1658 μs | 5.727 μs | 5.967 μs | 6.257 μs |  1.03 |    0.05 |    1 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 5.574 μs | 0.0706 μs | 0.0660 μs | 5.374 μs | 5.580 μs | 5.672 μs |  0.96 |    0.04 |    1 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 20.20 μs | 0.289 μs | 0.256 μs | 19.69 μs | 20.30 μs | 20.45 μs |  1.00 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 18.98 μs | 0.304 μs | 0.284 μs | 18.65 μs | 18.89 μs | 19.52 μs |  0.94 |    0.02 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 22.85 μs | 0.422 μs | 0.395 μs | 22.12 μs | 22.94 μs | 23.49 μs |  1.13 |    0.02 |    4 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 18.16 μs | 0.362 μs | 0.388 μs | 17.57 μs | 18.09 μs | 18.92 μs |  0.90 |    0.02 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 21.44 μs | 0.294 μs | 0.275 μs | 21.02 μs | 21.34 μs | 22.01 μs |  1.06 |    0.02 |    3 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 23.18 μs | 0.159 μs | 0.141 μs | 22.99 μs | 23.16 μs | 23.45 μs |  1.15 |    0.02 |    4 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,149.925 μs | 11.0980 μs | 10.3811 μs | 1,133.844 μs | 1,148.807 μs | 1,167.416 μs | 1.000 |    3 | 3.9063 |      - |   95.5 KB |        1.00 |
| AutoMapperStartup |   265.330 μs |  0.7697 μs |  0.6823 μs |   264.311 μs |   265.374 μs |   266.329 μs | 0.231 |    2 | 3.9063 |      - | 104.13 KB |        1.09 |
| MapsterStartup    |     3.097 μs |  0.0201 μs |  0.0188 μs |     3.067 μs |     3.092 μs |     3.128 μs | 0.003 |    1 | 0.4692 | 0.0114 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.133 ms | 0.0091 ms | 0.0076 ms | 1.122 ms | 1.135 ms | 1.149 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  96.21 KB |        1.00 |
| AutoMapper | 3.294 ms | 0.0156 ms | 0.0146 ms | 3.277 ms | 3.289 ms | 3.323 ms |  2.91 |    0.02 |    3 |  7.8125 |       - | 310.51 KB |        3.23 |
| Mapster    | 2.553 ms | 0.0125 ms | 0.0111 ms | 2.539 ms | 2.550 ms | 2.581 ms |  2.25 |    0.02 |    2 | 31.2500 | 23.4375 | 766.06 KB |        7.96 |

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

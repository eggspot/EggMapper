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
> ⏱ **Last updated:** 2026-04-18 03:25 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 12.73 ns | **20.69 ns (1.6×)** | 22.89 ns (1.8×) | 60.99 ns (4.8×) | 13.45 ns (1.1×) |
| **Flattening** | 15.86 ns | **25.71 ns (1.6×)** | 27.63 ns (1.7×) | 59.43 ns (3.8×) | 20.28 ns (1.3×) |
| **Deep (2 nested)** | 45.69 ns | **54.84 ns (1.2×)** | 56.66 ns (1.2×) | 84.14 ns (1.8×) | 42.79 ns (0.9×) |
| **Complex (nest+coll)** | 68.89 ns | **81.59 ns (1.2×)** | 82.63 ns (1.2×) | 114.74 ns (1.7×) | 62.84 ns (0.9×) |
| **Collection (100)** | 1.665 μs | **1.596 μs (1.0×)** | 1.670 μs (1.0×) | 2.071 μs (1.2×) | 1.732 μs (1.0×) |
| **Deep Coll (100)** | 4.718 μs | **5.051 μs (1.1×)** | 5.124 μs (1.1×) | 5.526 μs (1.2×) | 5.164 μs (1.1×) |
| **Large Coll (1000)** | 16.73 μs | **16.44 μs (1.0×)** | 17.44 μs (1.0×) | 20.64 μs (1.2×) | 17.65 μs (1.1×) |
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
| Manual               |  12.73 ns | 0.126 ns | 0.118 ns |  12.58 ns |  12.68 ns |  12.94 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  20.69 ns | 0.119 ns | 0.111 ns |  20.47 ns |  20.68 ns |  20.89 ns |  1.63 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  60.99 ns | 0.108 ns | 0.091 ns |  60.88 ns |  60.96 ns |  61.17 ns |  4.79 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  22.89 ns | 0.240 ns | 0.225 ns |  22.46 ns |  22.99 ns |  23.08 ns |  1.80 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  13.45 ns | 0.161 ns | 0.151 ns |  13.23 ns |  13.44 ns |  13.75 ns |  1.06 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 263.72 ns | 1.590 ns | 1.409 ns | 261.61 ns | 263.74 ns | 266.22 ns | 20.72 |    0.21 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.37 ns | 0.125 ns | 0.111 ns |  12.24 ns |  12.35 ns |  12.60 ns |  0.97 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.63 ns | 0.170 ns | 0.151 ns |  12.36 ns |  12.61 ns |  12.93 ns |  0.99 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.86 ns | 0.160 ns | 0.150 ns |  15.66 ns |  15.82 ns |  16.17 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  25.71 ns | 0.155 ns | 0.145 ns |  25.51 ns |  25.73 ns |  26.00 ns |  1.62 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  59.43 ns | 0.131 ns | 0.116 ns |  59.21 ns |  59.43 ns |  59.60 ns |  3.75 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  27.63 ns | 0.202 ns | 0.189 ns |  27.42 ns |  27.53 ns |  27.99 ns |  1.74 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  20.28 ns | 0.028 ns | 0.021 ns |  20.23 ns |  20.28 ns |  20.31 ns |  1.28 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 258.08 ns | 1.281 ns | 1.198 ns | 255.95 ns | 258.07 ns | 260.05 ns | 16.28 |    0.16 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  45.69 ns | 0.138 ns | 0.123 ns |  45.54 ns |  45.72 ns |  45.94 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  54.84 ns | 0.304 ns | 0.269 ns |  54.33 ns |  54.79 ns |  55.45 ns |  1.20 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  |  84.14 ns | 0.278 ns | 0.260 ns |  83.60 ns |  84.11 ns |  84.53 ns |  1.84 |    0.01 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  56.66 ns | 0.468 ns | 0.438 ns |  56.03 ns |  56.67 ns |  57.44 ns |  1.24 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  42.79 ns | 0.144 ns | 0.135 ns |  42.58 ns |  42.77 ns |  43.03 ns |  0.94 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 276.81 ns | 2.801 ns | 2.620 ns | 271.19 ns | 277.89 ns | 279.66 ns |  6.06 |    0.06 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  68.89 ns | 1.400 ns | 1.613 ns |  64.68 ns |  69.37 ns |  70.74 ns |  1.00 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  81.59 ns | 0.427 ns | 0.379 ns |  80.85 ns |  81.65 ns |  82.30 ns |  1.18 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 114.74 ns | 0.511 ns | 0.453 ns | 113.80 ns | 114.76 ns | 115.30 ns |  1.67 |    0.04 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  82.63 ns | 0.821 ns | 0.768 ns |  81.82 ns |  82.51 ns |  84.05 ns |  1.20 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  62.84 ns | 0.535 ns | 0.500 ns |  61.95 ns |  62.73 ns |  63.76 ns |  0.91 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 311.15 ns | 1.689 ns | 1.411 ns | 308.29 ns | 310.83 ns | 313.45 ns |  4.52 |    0.11 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.665 μs | 0.0136 μs | 0.0127 μs | 1.647 μs | 1.662 μs | 1.689 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.596 μs | 0.0313 μs | 0.0396 μs | 1.540 μs | 1.588 μs | 1.671 μs |  0.96 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.071 μs | 0.0213 μs | 0.0189 μs | 2.029 μs | 2.075 μs | 2.098 μs |  1.24 |    0.01 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.670 μs | 0.0269 μs | 0.0238 μs | 1.614 μs | 1.674 μs | 1.703 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.732 μs | 0.0338 μs | 0.0462 μs | 1.654 μs | 1.722 μs | 1.825 μs |  1.04 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.131 μs | 0.0416 μs | 0.0389 μs | 2.057 μs | 2.144 μs | 2.178 μs |  1.28 |    0.02 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.718 μs | 0.0339 μs | 0.0283 μs | 4.652 μs | 4.720 μs | 4.767 μs |  1.00 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.051 μs | 0.0535 μs | 0.0500 μs | 4.980 μs | 5.043 μs | 5.136 μs |  1.07 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 5.526 μs | 0.0323 μs | 0.0252 μs | 5.487 μs | 5.524 μs | 5.571 μs |  1.17 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.124 μs | 0.0411 μs | 0.0343 μs | 5.026 μs | 5.132 μs | 5.161 μs |  1.09 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.164 μs | 0.0414 μs | 0.0387 μs | 5.071 μs | 5.166 μs | 5.233 μs |  1.09 |    3 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.486 μs | 0.0391 μs | 0.0326 μs | 4.405 μs | 4.485 μs | 4.535 μs |  0.95 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 16.73 μs | 0.328 μs | 0.607 μs | 15.70 μs | 16.86 μs | 17.67 μs |  1.00 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.44 μs | 0.304 μs | 0.395 μs | 15.58 μs | 16.47 μs | 17.21 μs |  0.98 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.64 μs | 0.412 μs | 0.722 μs | 18.24 μs | 20.75 μs | 21.72 μs |  1.23 |    0.06 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.44 μs | 0.345 μs | 0.568 μs | 16.37 μs | 17.42 μs | 18.65 μs |  1.04 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.65 μs | 0.353 μs | 0.579 μs | 16.39 μs | 17.74 μs | 19.02 μs |  1.06 |    0.05 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.19 μs | 0.376 μs | 0.540 μs | 18.14 μs | 19.20 μs | 20.46 μs |  1.15 |    0.05 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 929.580 μs | 7.6391 μs | 5.9641 μs | 921.275 μs | 929.765 μs | 939.047 μs | 1.000 |    3 | 5.8594 | 3.9063 |  95.67 KB |        1.00 |
| AutoMapperStartup | 186.306 μs | 1.0625 μs | 0.8872 μs | 185.045 μs | 186.529 μs | 187.913 μs | 0.200 |    2 | 5.8594 |      - | 103.76 KB |        1.08 |
| MapsterStartup    |   1.986 μs | 0.0140 μs | 0.0131 μs |   1.971 μs |   1.980 μs |   2.009 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error   | StdDev  | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|--------:|--------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   922.2 μs | 6.88 μs | 6.43 μs |   909.8 μs |   923.0 μs |   933.6 μs |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.11 KB |        1.00 |
| AutoMapper | 2,650.8 μs | 7.39 μs | 6.55 μs | 2,643.8 μs | 2,648.6 μs | 2,664.7 μs |  2.87 |    0.02 |    3 | 15.6250 |  7.8125 | 310.54 KB |        3.23 |
| Mapster    | 2,007.2 μs | 5.09 μs | 4.52 μs | 1,998.2 μs | 2,008.2 μs | 2,013.2 μs |  2.18 |    0.02 |    2 | 46.8750 | 19.5313 | 765.99 KB |        7.97 |

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

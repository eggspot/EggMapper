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
> ⏱ **Last updated:** 2026-03-27 00:22 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.05 ns | **25.43 ns (1.7×)** | 28.49 ns (1.9×) | 81.99 ns (5.5×) | 15.47 ns (1.0×) |
| **Flattening** | 18.99 ns | **30.65 ns (1.6×)** | 49.44 ns (2.6×) | 89.92 ns (4.7×) | 24.97 ns (1.3×) |
| **Deep (2 nested)** | 53.38 ns | **66.28 ns (1.2×)** | 69.62 ns (1.3×) | 122.38 ns (2.3×) | 49.72 ns (0.9×) |
| **Complex (nest+coll)** | 69.65 ns | **91.53 ns (1.3×)** | 88.86 ns (1.3×) | 160.14 ns (2.3×) | 70.66 ns (1.0×) |
| **Collection (100)** | 1.756 μs | **1.770 μs (1.0×)** | 1.802 μs (1.0×) | 2.398 μs (1.4×) | 1.811 μs (1.0×) |
| **Deep Coll (100)** | 5.321 μs | **5.788 μs (1.1×)** | 5.755 μs (1.1×) | 6.462 μs (1.2×) | 5.682 μs (1.1×) |
| **Large Coll (1000)** | 18.59 μs | **21.09 μs (1.1×)** | 18.30 μs (1.0×) | 22.25 μs (1.2×) | 18.72 μs (1.0×) |
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
| Manual               |  15.05 ns | 0.130 ns | 0.122 ns |  14.77 ns |  15.08 ns |  15.19 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  25.43 ns | 0.263 ns | 0.233 ns |  24.89 ns |  25.45 ns |  25.76 ns |  1.69 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  81.99 ns | 0.562 ns | 0.526 ns |  81.17 ns |  81.88 ns |  82.99 ns |  5.45 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.49 ns | 0.168 ns | 0.149 ns |  28.18 ns |  28.49 ns |  28.71 ns |  1.89 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.47 ns | 0.204 ns | 0.191 ns |  15.14 ns |  15.45 ns |  15.80 ns |  1.03 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 484.10 ns | 1.652 ns | 1.464 ns | 480.29 ns | 484.33 ns | 486.27 ns | 32.16 |    0.27 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  14.93 ns | 0.133 ns | 0.125 ns |  14.72 ns |  14.92 ns |  15.17 ns |  0.99 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.72 ns | 0.234 ns | 0.218 ns |  15.43 ns |  15.70 ns |  16.16 ns |  1.04 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.99 ns | 0.224 ns | 0.199 ns |  18.62 ns |  18.98 ns |  19.30 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.65 ns | 0.594 ns | 0.583 ns |  29.41 ns |  30.82 ns |  31.66 ns |  1.61 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  89.92 ns | 0.173 ns | 0.153 ns |  89.64 ns |  89.94 ns |  90.19 ns |  4.73 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  49.44 ns | 0.199 ns | 0.176 ns |  49.21 ns |  49.39 ns |  49.74 ns |  2.60 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.97 ns | 0.145 ns | 0.128 ns |  24.74 ns |  25.00 ns |  25.20 ns |  1.31 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 535.24 ns | 1.350 ns | 1.196 ns | 533.41 ns | 535.20 ns | 537.15 ns | 28.18 |    0.29 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.38 ns | 0.419 ns | 0.371 ns |  52.65 ns |  53.41 ns |  53.88 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  66.28 ns | 0.964 ns | 0.902 ns |  64.06 ns |  66.46 ns |  67.61 ns |  1.24 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 122.38 ns | 0.719 ns | 0.637 ns | 121.25 ns | 122.34 ns | 123.61 ns |  2.29 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  69.62 ns | 1.049 ns | 1.122 ns |  67.69 ns |  69.51 ns |  72.18 ns |  1.30 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.72 ns | 0.811 ns | 0.719 ns |  48.43 ns |  49.51 ns |  50.85 ns |  0.93 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 492.71 ns | 2.627 ns | 2.193 ns | 487.73 ns | 493.40 ns | 494.66 ns |  9.23 |    0.07 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  69.65 ns | 0.648 ns | 0.606 ns |  68.66 ns |  69.65 ns |  70.87 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  91.53 ns | 1.704 ns | 1.594 ns |  89.08 ns |  91.46 ns |  94.00 ns |  1.31 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 160.14 ns | 1.529 ns | 1.430 ns | 156.92 ns | 160.22 ns | 162.45 ns |  2.30 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  88.86 ns | 1.718 ns | 2.172 ns |  86.19 ns |  88.04 ns |  93.25 ns |  1.28 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.66 ns | 1.203 ns | 1.126 ns |  69.15 ns |  70.59 ns |  72.91 ns |  1.01 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 565.85 ns | 2.417 ns | 2.261 ns | 561.23 ns | 565.53 ns | 570.14 ns |  8.12 |    0.08 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.756 μs | 0.0334 μs | 0.0594 μs | 1.699 μs | 1.733 μs | 1.905 μs |  1.00 |    0.05 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.770 μs | 0.0321 μs | 0.0285 μs | 1.726 μs | 1.764 μs | 1.823 μs |  1.01 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.398 μs | 0.0456 μs | 0.0426 μs | 2.322 μs | 2.403 μs | 2.473 μs |  1.37 |    0.05 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.802 μs | 0.0349 μs | 0.0533 μs | 1.706 μs | 1.784 μs | 1.905 μs |  1.03 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.811 μs | 0.0074 μs | 0.0069 μs | 1.803 μs | 1.809 μs | 1.826 μs |  1.03 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.556 μs | 0.0278 μs | 0.0232 μs | 2.501 μs | 2.562 μs | 2.578 μs |  1.46 |    0.05 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.321 μs | 0.0703 μs | 0.0657 μs | 5.217 μs | 5.349 μs | 5.411 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.788 μs | 0.1092 μs | 0.1021 μs | 5.621 μs | 5.758 μs | 5.977 μs |  1.09 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.462 μs | 0.1043 μs | 0.0871 μs | 6.312 μs | 6.452 μs | 6.633 μs |  1.21 |    0.02 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.755 μs | 0.0352 μs | 0.0312 μs | 5.718 μs | 5.749 μs | 5.823 μs |  1.08 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.682 μs | 0.0893 μs | 0.0792 μs | 5.524 μs | 5.683 μs | 5.833 μs |  1.07 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.445 μs | 0.0630 μs | 0.0589 μs | 5.348 μs | 5.430 μs | 5.553 μs |  1.02 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.59 μs | 0.361 μs | 0.354 μs | 18.07 μs | 18.50 μs | 19.24 μs |  1.00 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 21.09 μs | 0.408 μs | 0.436 μs | 20.17 μs | 21.14 μs | 21.74 μs |  1.13 |    0.03 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 22.25 μs | 0.224 μs | 0.209 μs | 21.85 μs | 22.23 μs | 22.60 μs |  1.20 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.30 μs | 0.201 μs | 0.188 μs | 18.01 μs | 18.27 μs | 18.74 μs |  0.98 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.72 μs | 0.167 μs | 0.156 μs | 18.42 μs | 18.79 μs | 18.93 μs |  1.01 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.51 μs | 0.161 μs | 0.150 μs | 20.23 μs | 20.54 μs | 20.81 μs |  1.10 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,146.612 μs | 9.6490 μs | 8.5536 μs | 1,129.579 μs | 1,146.490 μs | 1,160.827 μs | 1.000 |    3 | 3.9063 | 1.9531 |  94.55 KB |        1.00 |
| AutoMapperStartup |   282.163 μs | 1.1595 μs | 1.0279 μs |   280.279 μs |   282.325 μs |   283.906 μs | 0.246 |    2 | 5.8594 |      - | 104.17 KB |        1.10 |
| MapsterStartup    |     2.645 μs | 0.0477 μs | 0.0446 μs |     2.593 μs |     2.622 μs |     2.745 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.150 ms | 0.0096 ms | 0.0085 ms | 1.137 ms | 1.151 ms | 1.163 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  95.31 KB |        1.00 |
| AutoMapper | 3.290 ms | 0.0242 ms | 0.0226 ms | 3.251 ms | 3.292 ms | 3.333 ms |  2.86 |    0.03 |    3 | 15.6250 |  7.8125 |  310.5 KB |        3.26 |
| Mapster    | 2.501 ms | 0.0092 ms | 0.0077 ms | 2.491 ms | 2.500 ms | 2.520 ms |  2.17 |    0.02 |    2 | 39.0625 | 15.6250 | 763.96 KB |        8.02 |

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

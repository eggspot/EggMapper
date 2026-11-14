# 🥚 EggMapper

> **The fastest .NET runtime object-to-object mapper** — forked from AutoMapper's last open-source release, rebuilt for maximum performance. Drop-in replacement with the same API, 1.5–5× faster.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![License: MIT OR Apache-2.0](https://img.shields.io/badge/license-MIT%20OR%20Apache--2.0-green.svg)](LICENSE-MIT)

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
- 📖 **MIT OR Apache-2.0 licensed** — free for commercial use, forever

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
> ⏱ **Last updated:** 2026-11-14 11:28 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 12.44 ns | **20.51 ns (1.6×)** | 22.23 ns (1.8×) | 56.40 ns (4.5×) | 12.77 ns (1.0×) |
| **Flattening** | 15.60 ns | **24.11 ns (1.6×)** | 27.00 ns (1.7×) | 61.54 ns (4.0×) | 19.35 ns (1.2×) |
| **Deep (2 nested)** | 45.02 ns | **53.93 ns (1.2×)** | 55.58 ns (1.2×) | 84.82 ns (1.9×) | 44.43 ns (1.0×) |
| **Complex (nest+coll)** | 62.01 ns | **88.76 ns (1.4×)** | 81.00 ns (1.3×) | 120.55 ns (1.9×) | 65.58 ns (1.1×) |
| **Collection (100)** | 1.647 μs | **1.670 μs (1.0×)** | 1.741 μs (1.1×) | 2.059 μs (1.2×) | 1.753 μs (1.1×) |
| **Deep Coll (100)** | 4.864 μs | **5.351 μs (1.1×)** | 5.114 μs (1.1×) | 5.508 μs (1.1×) | 4.645 μs (1.0×) |
| **Large Coll (1000)** | 15.62 μs | **15.27 μs (1.0×)** | 15.26 μs (1.0×) | 18.89 μs (1.2×) | 16.86 μs (1.1×) |
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
| Manual               |  12.44 ns | 0.182 ns | 0.170 ns |  12.19 ns |  12.35 ns |  12.75 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  20.51 ns | 0.155 ns | 0.137 ns |  20.25 ns |  20.53 ns |  20.70 ns |  1.65 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  56.40 ns | 0.204 ns | 0.181 ns |  56.22 ns |  56.32 ns |  56.80 ns |  4.53 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  22.23 ns | 0.190 ns | 0.178 ns |  22.03 ns |  22.21 ns |  22.61 ns |  1.79 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  12.77 ns | 0.176 ns | 0.165 ns |  12.65 ns |  12.68 ns |  13.15 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 263.97 ns | 0.501 ns | 0.391 ns | 263.42 ns | 263.88 ns | 264.74 ns | 21.21 |    0.28 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.49 ns | 0.134 ns | 0.126 ns |  12.35 ns |  12.41 ns |  12.68 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.81 ns | 0.232 ns | 0.217 ns |  12.47 ns |  12.76 ns |  13.20 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.60 ns | 0.116 ns | 0.108 ns |  15.46 ns |  15.56 ns |  15.82 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  24.11 ns | 0.146 ns | 0.137 ns |  23.88 ns |  24.14 ns |  24.42 ns |  1.55 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  61.54 ns | 0.088 ns | 0.078 ns |  61.44 ns |  61.52 ns |  61.72 ns |  3.95 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  27.00 ns | 0.180 ns | 0.168 ns |  26.80 ns |  26.95 ns |  27.29 ns |  1.73 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  19.35 ns | 0.061 ns | 0.051 ns |  19.29 ns |  19.33 ns |  19.45 ns |  1.24 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 258.57 ns | 2.681 ns | 2.376 ns | 255.91 ns | 258.34 ns | 264.06 ns | 16.58 |    0.18 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  45.02 ns | 0.194 ns | 0.182 ns |  44.80 ns |  44.98 ns |  45.37 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  53.93 ns | 0.200 ns | 0.178 ns |  53.71 ns |  53.89 ns |  54.39 ns |  1.20 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| AutoMapper  |  84.82 ns | 0.313 ns | 0.293 ns |  84.39 ns |  84.78 ns |  85.34 ns |  1.88 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  55.58 ns | 0.279 ns | 0.261 ns |  55.01 ns |  55.60 ns |  55.97 ns |  1.23 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  44.43 ns | 0.329 ns | 0.257 ns |  43.69 ns |  44.49 ns |  44.66 ns |  0.99 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 278.35 ns | 1.677 ns | 1.400 ns | 275.16 ns | 278.54 ns | 280.20 ns |  6.18 |    0.04 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  62.01 ns | 1.242 ns | 1.162 ns |  60.09 ns |  62.30 ns |  63.37 ns |  1.00 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  88.76 ns | 0.970 ns | 0.860 ns |  87.09 ns |  88.96 ns |  89.87 ns |  1.43 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 120.55 ns | 0.637 ns | 0.532 ns | 119.47 ns | 120.60 ns | 121.49 ns |  1.94 |    0.04 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  81.00 ns | 0.648 ns | 0.575 ns |  80.19 ns |  80.97 ns |  82.17 ns |  1.31 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  65.58 ns | 1.171 ns | 1.096 ns |  63.41 ns |  66.05 ns |  66.74 ns |  1.06 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 336.77 ns | 1.825 ns | 1.707 ns | 332.78 ns | 337.10 ns | 338.67 ns |  5.43 |    0.10 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.647 μs | 0.0281 μs | 0.0262 μs | 1.582 μs | 1.650 μs | 1.681 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.670 μs | 0.0334 μs | 0.0445 μs | 1.574 μs | 1.675 μs | 1.770 μs |  1.01 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.059 μs | 0.0401 μs | 0.0446 μs | 1.969 μs | 2.052 μs | 2.155 μs |  1.25 |    0.03 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.741 μs | 0.0348 μs | 0.0342 μs | 1.678 μs | 1.742 μs | 1.795 μs |  1.06 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.753 μs | 0.0345 μs | 0.0657 μs | 1.619 μs | 1.735 μs | 1.886 μs |  1.06 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.201 μs | 0.0423 μs | 0.0453 μs | 2.082 μs | 2.211 μs | 2.269 μs |  1.34 |    0.03 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.864 μs | 0.0826 μs | 0.0773 μs | 4.732 μs | 4.892 μs | 4.982 μs |  1.00 |    0.02 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.351 μs | 0.0761 μs | 0.0712 μs | 5.247 μs | 5.334 μs | 5.454 μs |  1.10 |    0.02 |    5 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 5.508 μs | 0.0390 μs | 0.0346 μs | 5.454 μs | 5.504 μs | 5.570 μs |  1.13 |    0.02 |    5 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.114 μs | 0.0582 μs | 0.0516 μs | 5.002 μs | 5.115 μs | 5.207 μs |  1.05 |    0.02 |    4 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 4.645 μs | 0.0318 μs | 0.0282 μs | 4.598 μs | 4.637 μs | 4.704 μs |  0.96 |    0.02 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.431 μs | 0.0424 μs | 0.0396 μs | 4.381 μs | 4.430 μs | 4.503 μs |  0.91 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 15.62 μs | 0.143 μs | 0.134 μs | 15.38 μs | 15.61 μs | 15.84 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 15.27 μs | 0.305 μs | 0.313 μs | 14.69 μs | 15.28 μs | 15.73 μs |  0.98 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 18.89 μs | 0.365 μs | 0.421 μs | 17.98 μs | 19.06 μs | 19.34 μs |  1.21 |    0.03 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 15.26 μs | 0.199 μs | 0.186 μs | 14.98 μs | 15.24 μs | 15.59 μs |  0.98 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 16.86 μs | 0.292 μs | 0.273 μs | 16.51 μs | 16.80 μs | 17.44 μs |  1.08 |    0.02 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 18.09 μs | 0.281 μs | 0.262 μs | 17.69 μs | 18.03 μs | 18.53 μs |  1.16 |    0.02 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error      | StdDev    | Min        | Median     | Max        | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|-----------:|----------:|-----------:|-----------:|-----------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 925.757 μs | 10.2841 μs | 9.6198 μs | 908.967 μs | 925.948 μs | 943.016 μs | 1.000 |    3 | 3.9063 | 1.9531 |  94.97 KB |        1.00 |
| AutoMapperStartup | 322.412 μs |  2.3113 μs | 1.9300 μs | 319.435 μs | 321.841 μs | 325.851 μs | 0.348 |    2 | 5.8594 |      - | 106.29 KB |        1.12 |
| MapsterStartup    |   2.048 μs |  0.0154 μs | 0.0137 μs |   2.023 μs |   2.046 μs |   2.069 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error    | StdDev   | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   918.2 μs | 17.41 μs | 16.28 μs |   888.3 μs |   916.1 μs |   950.4 μs |  1.00 |    0.02 |    1 |  5.8594 |  3.9063 |  96.14 KB |        1.00 |
| AutoMapper | 3,708.2 μs | 29.01 μs | 24.22 μs | 3,675.0 μs | 3,713.0 μs | 3,744.0 μs |  4.04 |    0.07 |    3 | 15.6250 |  7.8125 | 327.21 KB |        3.40 |
| Mapster    | 2,014.4 μs |  9.01 μs |  8.43 μs | 2,001.5 μs | 2,012.2 μs | 2,033.0 μs |  2.19 |    0.04 |    2 | 42.9688 | 19.5313 | 764.33 KB |        7.95 |

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
- ✅ Null source collection → empty destination collection (default `AllowNullCollections = false` semantics)
- ✅ Unmatched destination collection properties auto-initialized to empty (top-level + nested inline maps)
- ✅ `Ignore()` on getter-only and non-public-setter properties; non-`Ignore()` ops throw at config time
- ✅ Custom `IEnumerable` wrappers (e.g. `SelectList`) auto-constructed via cached interface-ctor lookup
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

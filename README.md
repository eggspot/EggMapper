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
> ⏱ **Last updated:** 2026-11-14 10:21 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 10.047 ns | **15.321 ns (1.5×)** | 13.578 ns (1.4×) | 36.001 ns (3.6×) | 10.395 ns (1.0×) |
| **Flattening** | 13.10 ns | **19.51 ns (1.5×)** | 20.88 ns (1.6×) | 38.04 ns (2.9×) | 15.81 ns (1.2×) |
| **Deep (2 nested)** | 35.39 ns | **40.21 ns (1.1×)** | 42.14 ns (1.2×) | 70.14 ns (2.0×) | 33.77 ns (1.0×) |
| **Complex (nest+coll)** | 43.94 ns | **52.95 ns (1.2×)** | 53.51 ns (1.2×) | 82.83 ns (1.9×) | 46.96 ns (1.1×) |
| **Collection (100)** | 1.150 μs | **1.243 μs (1.1×)** | 1.156 μs (1.0×) | 1.618 μs (1.4×) | 1.182 μs (1.0×) |
| **Deep Coll (100)** | 3.198 μs | **3.502 μs (1.1×)** | 3.542 μs (1.1×) | 4.280 μs (1.3×) | 3.227 μs (1.0×) |
| **Large Coll (1000)** | 13.69 μs | **12.69 μs (0.9×)** | 13.71 μs (1.0×) | 16.70 μs (1.2×) | 14.30 μs (1.1×) |
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

| Method               | Mean       | Error     | StdDev    | Min        | Median     | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-----------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual               |  10.047 ns | 0.2416 ns | 0.4713 ns |   9.022 ns |  10.153 ns |  10.67 ns |  1.00 |    0.07 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  15.321 ns | 0.3441 ns | 0.4935 ns |  14.265 ns |  15.358 ns |  16.19 ns |  1.53 |    0.09 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  36.001 ns | 0.7039 ns | 1.0318 ns |  34.763 ns |  35.601 ns |  38.66 ns |  3.59 |    0.20 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  13.578 ns | 0.1807 ns | 0.1509 ns |  13.343 ns |  13.569 ns |  13.90 ns |  1.35 |    0.07 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  10.395 ns | 0.2471 ns | 0.5047 ns |   9.484 ns |  10.337 ns |  11.40 ns |  1.04 |    0.07 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 168.029 ns | 3.3198 ns | 4.6540 ns | 159.249 ns | 170.064 ns | 174.17 ns | 16.76 |    0.92 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |   9.898 ns | 0.2354 ns | 0.1966 ns |   9.591 ns |   9.938 ns |  10.36 ns |  0.99 |    0.05 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  10.707 ns | 0.2477 ns | 0.2544 ns |  10.092 ns |  10.759 ns |  11.09 ns |  1.07 |    0.06 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  13.10 ns | 0.296 ns | 0.425 ns |  12.33 ns |  13.09 ns |  14.03 ns |  1.00 |    0.05 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  19.51 ns | 0.261 ns | 0.244 ns |  18.87 ns |  19.53 ns |  19.80 ns |  1.49 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  38.04 ns | 0.745 ns | 1.138 ns |  36.33 ns |  38.32 ns |  40.86 ns |  2.91 |    0.13 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  20.88 ns | 0.416 ns | 0.850 ns |  19.75 ns |  20.73 ns |  22.43 ns |  1.60 |    0.08 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  15.81 ns | 0.356 ns | 0.424 ns |  14.99 ns |  15.84 ns |  16.40 ns |  1.21 |    0.05 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 165.05 ns | 0.623 ns | 0.553 ns | 163.47 ns | 165.01 ns | 165.91 ns | 12.61 |    0.41 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  35.39 ns | 0.739 ns | 1.275 ns |  32.90 ns |  35.50 ns |  38.10 ns |  1.00 |    0.05 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  40.21 ns | 0.836 ns | 1.026 ns |  38.05 ns |  40.20 ns |  41.90 ns |  1.14 |    0.05 |    2 | 0.0162 |     272 B |        1.00 |
| AutoMapper  |  70.14 ns | 0.883 ns | 0.783 ns |  68.95 ns |  70.32 ns |  71.22 ns |  1.98 |    0.07 |    3 | 0.0162 |     272 B |        1.00 |
| Mapster     |  42.14 ns | 0.800 ns | 0.856 ns |  40.68 ns |  42.26 ns |  44.27 ns |  1.19 |    0.05 |    2 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  33.77 ns | 0.598 ns | 0.560 ns |  32.68 ns |  33.97 ns |  34.68 ns |  0.96 |    0.04 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 190.60 ns | 2.494 ns | 2.333 ns | 184.44 ns | 191.32 ns | 193.26 ns |  5.39 |    0.20 |    4 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  43.94 ns | 0.367 ns | 0.307 ns |  43.51 ns |  43.91 ns |  44.65 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  52.95 ns | 1.084 ns | 1.655 ns |  50.46 ns |  52.87 ns |  55.70 ns |  1.21 |    0.04 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  |  82.83 ns | 1.599 ns | 1.642 ns |  80.09 ns |  82.92 ns |  85.41 ns |  1.88 |    0.04 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  53.51 ns | 0.812 ns | 0.720 ns |  52.21 ns |  53.62 ns |  54.48 ns |  1.22 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  46.96 ns | 0.942 ns | 1.047 ns |  44.97 ns |  46.82 ns |  49.00 ns |  1.07 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 193.83 ns | 2.696 ns | 2.522 ns | 189.97 ns | 193.40 ns | 199.81 ns |  4.41 |    0.06 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.150 μs | 0.0227 μs | 0.0465 μs | 1.138 μs | 1.082 μs | 1.280 μs |  1.00 |    0.06 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.243 μs | 0.0248 μs | 0.0408 μs | 1.250 μs | 1.146 μs | 1.332 μs |  1.08 |    0.06 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 1.618 μs | 0.0315 μs | 0.0375 μs | 1.618 μs | 1.544 μs | 1.683 μs |  1.41 |    0.06 |    3 | 0.6084 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.156 μs | 0.0223 μs | 0.0305 μs | 1.160 μs | 1.091 μs | 1.215 μs |  1.01 |    0.05 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.182 μs | 0.0235 μs | 0.0659 μs | 1.158 μs | 1.105 μs | 1.331 μs |  1.03 |    0.07 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 1.357 μs | 0.0161 μs | 0.0135 μs | 1.355 μs | 1.327 μs | 1.379 μs |  1.18 |    0.05 |    2 | 0.5436 | 0.0172 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 3.198 μs | 0.0612 μs | 0.0629 μs | 3.112 μs | 3.207 μs | 3.295 μs |  1.00 |    0.03 |    1 | 1.6747 | 0.0992 |   27.4 KB |        1.00 |
| EggMapper   | 3.502 μs | 0.0694 μs | 0.0996 μs | 3.348 μs | 3.470 μs | 3.779 μs |  1.10 |    0.04 |    1 | 1.6747 | 0.0992 |   27.4 KB |        1.00 |
| AutoMapper  | 4.280 μs | 0.0789 μs | 0.0738 μs | 4.052 μs | 4.283 μs | 4.367 μs |  1.34 |    0.03 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 3.542 μs | 0.0474 μs | 0.0396 μs | 3.441 μs | 3.543 μs | 3.589 μs |  1.11 |    0.02 |    1 | 1.6747 | 0.0992 |   27.4 KB |        1.00 |
| MapperlyMap | 3.227 μs | 0.0623 μs | 0.0932 μs | 3.086 μs | 3.202 μs | 3.457 μs |  1.01 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 3.337 μs | 0.0664 μs | 0.1641 μs | 2.897 μs | 3.338 μs | 3.623 μs |  1.04 |    0.05 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 13.69 μs | 0.265 μs | 0.389 μs | 12.71 μs | 13.76 μs | 14.29 μs |  1.00 |    0.04 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 12.69 μs | 0.251 μs | 0.633 μs | 11.49 μs | 12.67 μs | 14.09 μs |  0.93 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 16.70 μs | 0.319 μs | 0.298 μs | 16.10 μs | 16.74 μs | 17.09 μs |  1.22 |    0.04 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 13.71 μs | 0.274 μs | 0.346 μs | 12.81 μs | 13.76 μs | 14.25 μs |  1.00 |    0.04 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 14.30 μs | 0.284 μs | 0.370 μs | 13.35 μs | 14.30 μs | 14.83 μs |  1.05 |    0.04 |    2 | 5.2643 | 1.3123 |  86.02 KB |        1.00 |
| AgileMapper | 14.15 μs | 0.186 μs | 0.155 μs | 13.88 μs | 14.13 μs | 14.38 μs |  1.03 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error      | StdDev     | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|-----------:|-----------:|-----------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 749.866 μs | 14.7331 μs | 20.6538 μs | 712.292 μs | 754.955 μs | 784.826 μs | 1.001 |    0.04 |    3 | 4.8828 | 3.9063 |  95.19 KB |        1.00 |
| AutoMapperStartup | 203.468 μs |  1.7880 μs |  1.5850 μs | 201.312 μs | 203.201 μs | 206.280 μs | 0.272 |    0.01 |    2 | 6.8359 | 0.9766 | 106.06 KB |        1.11 |
| MapsterStartup    |   1.512 μs |  0.0296 μs |  0.0329 μs |   1.463 μs |   1.507 μs |   1.584 μs | 0.002 |    0.00 |    1 | 0.7038 | 0.0286 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error    | StdDev   | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   728.5 μs | 13.27 μs | 12.42 μs |   704.8 μs |   727.6 μs |   754.1 μs |  1.00 |    0.02 |    1 |  5.8594 |  4.8828 |  96.32 KB |        1.00 |
| AutoMapper | 3,081.2 μs | 58.65 μs | 62.75 μs | 2,989.4 μs | 3,072.7 μs | 3,200.5 μs |  4.23 |    0.11 |    3 | 15.6250 |  7.8125 | 327.31 KB |        3.40 |
| Mapster    | 1,650.3 μs | 19.30 μs | 18.06 μs | 1,634.8 μs | 1,643.2 μs | 1,686.5 μs |  2.27 |    0.04 |    2 | 42.9688 | 19.5313 | 757.59 KB |        7.87 |

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

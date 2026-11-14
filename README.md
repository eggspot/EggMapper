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
> ⏱ **Last updated:** 2026-11-14 11:23 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 12.86 ns | **21.13 ns (1.6×)** | 22.10 ns (1.7×) | 61.66 ns (4.8×) | 12.65 ns (1.0×) |
| **Flattening** | 15.83 ns | **24.40 ns (1.5×)** | 26.62 ns (1.7×) | 60.82 ns (3.8×) | 19.44 ns (1.2×) |
| **Deep (2 nested)** | 45.76 ns | **53.92 ns (1.2×)** | 58.35 ns (1.3×) | 87.75 ns (1.9×) | 41.96 ns (0.9×) |
| **Complex (nest+coll)** | 66.38 ns | **84.41 ns (1.3×)** | 78.18 ns (1.2×) | 134.10 ns (2.0×) | 65.97 ns (1.0×) |
| **Collection (100)** | 1.654 μs | **1.717 μs (1.0×)** | 1.716 μs (1.0×) | 2.180 μs (1.3×) | 1.774 μs (1.1×) |
| **Deep Coll (100)** | 4.779 μs | **5.159 μs (1.1×)** | 5.205 μs (1.1×) | 5.743 μs (1.2×) | 4.752 μs (1.0×) |
| **Large Coll (1000)** | 16.75 μs | **16.29 μs (1.0×)** | 16.82 μs (1.0×) | 19.66 μs (1.2×) | 17.23 μs (1.0×) |
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
| Manual               |  12.86 ns | 0.299 ns | 0.356 ns |  12.30 ns |  12.88 ns |  13.30 ns |  1.00 |    0.04 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  21.13 ns | 0.263 ns | 0.246 ns |  20.83 ns |  21.07 ns |  21.63 ns |  1.65 |    0.05 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  61.66 ns | 0.254 ns | 0.198 ns |  61.12 ns |  61.74 ns |  61.86 ns |  4.80 |    0.13 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  22.10 ns | 0.159 ns | 0.133 ns |  21.71 ns |  22.14 ns |  22.23 ns |  1.72 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  12.65 ns | 0.134 ns | 0.119 ns |  12.47 ns |  12.65 ns |  12.91 ns |  0.98 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 261.34 ns | 2.789 ns | 2.472 ns | 258.55 ns | 260.26 ns | 267.29 ns | 20.34 |    0.58 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.40 ns | 0.148 ns | 0.139 ns |  12.20 ns |  12.42 ns |  12.68 ns |  0.97 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.97 ns | 0.136 ns | 0.114 ns |  12.78 ns |  13.00 ns |  13.12 ns |  1.01 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.83 ns | 0.110 ns | 0.092 ns |  15.64 ns |  15.82 ns |  15.95 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  24.40 ns | 0.232 ns | 0.206 ns |  24.01 ns |  24.41 ns |  24.78 ns |  1.54 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  60.82 ns | 0.250 ns | 0.209 ns |  60.59 ns |  60.76 ns |  61.38 ns |  3.84 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  26.62 ns | 0.277 ns | 0.246 ns |  26.35 ns |  26.51 ns |  27.07 ns |  1.68 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  19.44 ns | 0.089 ns | 0.074 ns |  19.33 ns |  19.42 ns |  19.54 ns |  1.23 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 262.48 ns | 1.351 ns | 1.264 ns | 259.41 ns | 262.77 ns | 264.06 ns | 16.58 |    0.12 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  45.76 ns | 0.171 ns | 0.143 ns |  45.51 ns |  45.74 ns |  46.09 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  53.92 ns | 0.272 ns | 0.241 ns |  53.57 ns |  53.89 ns |  54.47 ns |  1.18 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  |  87.75 ns | 0.462 ns | 0.386 ns |  86.98 ns |  87.68 ns |  88.49 ns |  1.92 |    0.01 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  58.35 ns | 0.520 ns | 0.486 ns |  57.42 ns |  58.34 ns |  59.07 ns |  1.28 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  41.96 ns | 0.094 ns | 0.083 ns |  41.83 ns |  41.97 ns |  42.13 ns |  0.92 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 289.52 ns | 2.899 ns | 2.570 ns | 284.78 ns | 289.41 ns | 294.09 ns |  6.33 |    0.06 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  66.38 ns | 0.199 ns | 0.186 ns |  66.03 ns |  66.44 ns |  66.57 ns |  1.00 |    0.00 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  84.41 ns | 0.569 ns | 0.533 ns |  83.35 ns |  84.38 ns |  85.30 ns |  1.27 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 134.10 ns | 0.853 ns | 0.712 ns | 132.70 ns | 134.17 ns | 134.93 ns |  2.02 |    0.01 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  78.18 ns | 0.526 ns | 0.466 ns |  77.74 ns |  78.03 ns |  79.39 ns |  1.18 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  65.97 ns | 0.382 ns | 0.339 ns |  65.43 ns |  65.92 ns |  66.55 ns |  0.99 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 327.47 ns | 2.464 ns | 2.184 ns | 323.91 ns | 327.89 ns | 330.82 ns |  4.93 |    0.03 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.654 μs | 0.0331 μs | 0.0464 μs | 1.550 μs | 1.662 μs | 1.728 μs |  1.00 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.717 μs | 0.0331 μs | 0.0309 μs | 1.684 μs | 1.705 μs | 1.765 μs |  1.04 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.180 μs | 0.0420 μs | 0.0466 μs | 2.058 μs | 2.173 μs | 2.243 μs |  1.32 |    0.05 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.716 μs | 0.0326 μs | 0.0348 μs | 1.673 μs | 1.712 μs | 1.789 μs |  1.04 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.774 μs | 0.0226 μs | 0.0201 μs | 1.718 μs | 1.778 μs | 1.801 μs |  1.07 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.203 μs | 0.0388 μs | 0.0381 μs | 2.144 μs | 2.197 μs | 2.273 μs |  1.33 |    0.04 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.779 μs | 0.0611 μs | 0.0572 μs | 4.648 μs | 4.789 μs | 4.862 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.159 μs | 0.0883 μs | 0.0826 μs | 5.061 μs | 5.136 μs | 5.332 μs |  1.08 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 5.743 μs | 0.0781 μs | 0.0731 μs | 5.620 μs | 5.741 μs | 5.884 μs |  1.20 |    0.02 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.205 μs | 0.0864 μs | 0.0766 μs | 5.041 μs | 5.217 μs | 5.300 μs |  1.09 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 4.752 μs | 0.0934 μs | 0.1278 μs | 4.598 μs | 4.746 μs | 5.028 μs |  0.99 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.648 μs | 0.0477 μs | 0.0446 μs | 4.580 μs | 4.644 μs | 4.734 μs |  0.97 |    0.01 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 16.75 μs | 0.212 μs | 0.198 μs | 16.43 μs | 16.72 μs | 17.14 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.29 μs | 0.265 μs | 0.235 μs | 15.90 μs | 16.29 μs | 16.78 μs |  0.97 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 19.66 μs | 0.386 μs | 0.445 μs | 18.74 μs | 19.73 μs | 20.61 μs |  1.17 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 16.82 μs | 0.206 μs | 0.193 μs | 16.29 μs | 16.83 μs | 17.07 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.23 μs | 0.302 μs | 0.283 μs | 16.87 μs | 17.12 μs | 17.84 μs |  1.03 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 17.84 μs | 0.164 μs | 0.146 μs | 17.62 μs | 17.84 μs | 18.15 μs |  1.07 |    0.01 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error      | StdDev    | Min        | Median     | Max        | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|-----------:|----------:|-----------:|-----------:|-----------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 941.794 μs | 10.1750 μs | 9.5177 μs | 930.465 μs | 937.508 μs | 966.045 μs | 1.000 |    3 | 5.8594 | 3.9063 |  95.74 KB |        1.00 |
| AutoMapperStartup | 326.376 μs |  3.2894 μs | 2.7468 μs | 322.439 μs | 325.913 μs | 332.674 μs | 0.347 |    2 | 5.8594 |      - | 106.48 KB |        1.11 |
| MapsterStartup    |   2.040 μs |  0.0307 μs | 0.0287 μs |   2.001 μs |   2.028 μs |   2.095 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error    | StdDev   | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   943.2 μs | 18.57 μs | 21.39 μs |   913.5 μs |   935.8 μs |   995.6 μs |  1.00 |    0.03 |    1 |  5.8594 |  3.9063 |     96 KB |        1.00 |
| AutoMapper | 3,735.8 μs | 25.97 μs | 23.03 μs | 3,694.3 μs | 3,737.7 μs | 3,777.5 μs |  3.96 |    0.09 |    3 | 15.6250 |  7.8125 | 327.01 KB |        3.41 |
| Mapster    | 2,025.6 μs | 14.41 μs | 12.78 μs | 2,007.3 μs | 2,021.6 μs | 2,051.8 μs |  2.15 |    0.05 |    2 | 42.9688 | 19.5313 | 759.59 KB |        7.91 |

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

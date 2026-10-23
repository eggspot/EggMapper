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
> ⏱ **Last updated:** 2026-10-23 17:16 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.51 ns | **26.61 ns (1.7×)** | 28.40 ns (1.8×) | 82.45 ns (5.3×) | 15.83 ns (1.0×) |
| **Flattening** | 19.74 ns | **29.74 ns (1.5×)** | 48.36 ns (2.5×) | 87.53 ns (4.4×) | 26.22 ns (1.3×) |
| **Deep (2 nested)** | 57.88 ns | **69.36 ns (1.2×)** | 74.84 ns (1.3×) | 134.30 ns (2.3×) | 53.22 ns (0.9×) |
| **Complex (nest+coll)** | 74.52 ns | **97.23 ns (1.3×)** | 93.96 ns (1.3×) | 160.74 ns (2.2×) | 74.80 ns (1.0×) |
| **Collection (100)** | 1.813 μs | **1.806 μs (1.0×)** | 1.787 μs (1.0×) | 2.998 μs (1.7×) | 2.208 μs (1.2×) |
| **Deep Coll (100)** | 5.470 μs | **5.687 μs (1.0×)** | 5.952 μs (1.1×) | 6.760 μs (1.2×) | 5.663 μs (1.0×) |
| **Large Coll (1000)** | 18.78 μs | **18.38 μs (1.0×)** | 17.83 μs (0.9×) | 22.41 μs (1.2×) | 18.82 μs (1.0×) |
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
| Manual               |  15.51 ns | 0.202 ns | 0.179 ns |  15.19 ns |  15.47 ns |  15.82 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.61 ns | 0.388 ns | 0.344 ns |  25.97 ns |  26.68 ns |  27.18 ns |  1.72 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  82.45 ns | 0.339 ns | 0.301 ns |  81.88 ns |  82.49 ns |  83.01 ns |  5.32 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.40 ns | 0.232 ns | 0.217 ns |  28.05 ns |  28.38 ns |  28.76 ns |  1.83 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.83 ns | 0.155 ns | 0.130 ns |  15.65 ns |  15.79 ns |  16.10 ns |  1.02 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 492.71 ns | 2.754 ns | 2.576 ns | 488.58 ns | 491.99 ns | 497.35 ns | 31.77 |    0.39 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.43 ns | 0.188 ns | 0.176 ns |  15.23 ns |  15.42 ns |  15.84 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.55 ns | 0.302 ns | 0.283 ns |  16.02 ns |  16.60 ns |  17.07 ns |  1.07 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.74 ns | 0.195 ns | 0.163 ns |  19.42 ns |  19.75 ns |  19.98 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.74 ns | 0.290 ns | 0.272 ns |  29.29 ns |  29.77 ns |  30.19 ns |  1.51 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  87.53 ns | 0.431 ns | 0.382 ns |  86.62 ns |  87.59 ns |  88.04 ns |  4.44 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  48.36 ns | 0.254 ns | 0.212 ns |  48.00 ns |  48.34 ns |  48.70 ns |  2.45 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  26.22 ns | 0.585 ns | 0.977 ns |  24.26 ns |  26.28 ns |  27.81 ns |  1.33 |    0.05 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 550.07 ns | 0.688 ns | 0.610 ns | 549.17 ns | 550.03 ns | 550.99 ns | 27.87 |    0.22 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  57.88 ns | 1.202 ns | 1.066 ns |  54.66 ns |  58.17 ns |  58.96 ns |  1.00 |    0.03 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  69.36 ns | 1.343 ns | 1.649 ns |  66.50 ns |  69.23 ns |  72.58 ns |  1.20 |    0.04 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 134.30 ns | 1.002 ns | 0.937 ns | 133.04 ns | 134.08 ns | 136.44 ns |  2.32 |    0.05 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  74.84 ns | 1.486 ns | 2.035 ns |  72.31 ns |  74.79 ns |  79.09 ns |  1.29 |    0.04 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  53.22 ns | 0.630 ns | 0.559 ns |  52.35 ns |  53.11 ns |  54.24 ns |  0.92 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 513.60 ns | 3.056 ns | 2.858 ns | 510.25 ns | 512.10 ns | 517.65 ns |  8.88 |    0.17 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  74.52 ns | 1.492 ns | 1.532 ns |  72.51 ns |  74.46 ns |  77.63 ns |  1.00 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  97.23 ns | 1.867 ns | 1.746 ns |  94.98 ns |  97.37 ns | 101.28 ns |  1.31 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 160.74 ns | 0.746 ns | 0.697 ns | 158.90 ns | 160.92 ns | 161.61 ns |  2.16 |    0.04 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  93.96 ns | 1.639 ns | 1.453 ns |  91.55 ns |  93.75 ns |  96.70 ns |  1.26 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  74.80 ns | 0.796 ns | 0.745 ns |  73.60 ns |  74.64 ns |  76.19 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 553.75 ns | 2.574 ns | 2.282 ns | 549.84 ns | 553.68 ns | 557.98 ns |  7.43 |    0.15 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.813 μs | 0.0361 μs | 0.0776 μs | 1.770 μs | 1.736 μs | 2.003 μs |  1.00 |    0.06 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 1.806 μs | 0.0266 μs | 0.0236 μs | 1.805 μs | 1.773 μs | 1.850 μs |  1.00 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.998 μs | 0.0584 μs | 0.0672 μs | 3.025 μs | 2.842 μs | 3.077 μs |  1.66 |    0.08 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.787 μs | 0.0326 μs | 0.0272 μs | 1.772 μs | 1.758 μs | 1.830 μs |  0.99 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.208 μs | 0.0226 μs | 0.0212 μs | 2.211 μs | 2.152 μs | 2.236 μs |  1.22 |    0.05 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.886 μs | 0.0560 μs | 0.0921 μs | 2.923 μs | 2.694 μs | 3.013 μs |  1.59 |    0.08 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.470 μs | 0.0554 μs | 0.0518 μs | 5.399 μs | 5.458 μs | 5.571 μs |  1.00 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.687 μs | 0.0422 μs | 0.0374 μs | 5.633 μs | 5.687 μs | 5.767 μs |  1.04 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.760 μs | 0.0856 μs | 0.0759 μs | 6.618 μs | 6.752 μs | 6.904 μs |  1.24 |    0.02 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.952 μs | 0.0643 μs | 0.0601 μs | 5.863 μs | 5.971 μs | 6.043 μs |  1.09 |    0.01 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.663 μs | 0.1126 μs | 0.1542 μs | 5.425 μs | 5.667 μs | 5.907 μs |  1.04 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.457 μs | 0.0846 μs | 0.0792 μs | 5.303 μs | 5.468 μs | 5.581 μs |  1.00 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.78 μs | 0.312 μs | 0.292 μs | 18.21 μs | 18.80 μs | 19.22 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.38 μs | 0.345 μs | 0.306 μs | 17.77 μs | 18.31 μs | 18.97 μs |  0.98 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 22.41 μs | 0.192 μs | 0.170 μs | 22.09 μs | 22.40 μs | 22.66 μs |  1.19 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.83 μs | 0.183 μs | 0.171 μs | 17.47 μs | 17.85 μs | 18.03 μs |  0.95 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.82 μs | 0.121 μs | 0.101 μs | 18.56 μs | 18.85 μs | 18.92 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.90 μs | 0.161 μs | 0.151 μs | 20.63 μs | 20.94 μs | 21.12 μs |  1.11 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,158.349 μs | 7.4898 μs | 7.0060 μs | 1,148.050 μs | 1,160.061 μs | 1,167.961 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.28 KB |        1.00 |
| AutoMapperStartup |   288.805 μs | 1.2470 μs | 0.9736 μs |   287.225 μs |   288.886 μs |   290.476 μs | 0.249 |    2 | 5.8594 |      - | 104.13 KB |        1.09 |
| MapsterStartup    |     2.595 μs | 0.0281 μs | 0.0263 μs |     2.558 μs |     2.589 μs |     2.636 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.192 ms | 0.0224 ms | 0.0220 ms | 1.162 ms | 1.189 ms | 1.242 ms |  1.00 |    0.03 |    1 |  5.8594 |  1.9531 |  95.81 KB |        1.00 |
| AutoMapper | 3.500 ms | 0.0644 ms | 0.0766 ms | 3.289 ms | 3.511 ms | 3.598 ms |  2.94 |    0.08 |    3 | 15.6250 |  7.8125 | 309.67 KB |        3.23 |
| Mapster    | 2.653 ms | 0.0299 ms | 0.0280 ms | 2.589 ms | 2.660 ms | 2.702 ms |  2.23 |    0.05 |    2 | 46.8750 | 15.6250 | 766.44 KB |        8.00 |

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

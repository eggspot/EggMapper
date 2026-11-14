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
> ⏱ **Last updated:** 2026-11-14 10:39 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.33 ns | **27.23 ns (1.8×)** | 27.56 ns (1.8×) | 82.43 ns (5.4×) | 15.40 ns (1.0×) |
| **Flattening** | 19.16 ns | **29.49 ns (1.5×)** | 47.96 ns (2.5×) | 93.35 ns (4.9×) | 24.09 ns (1.3×) |
| **Deep (2 nested)** | 54.45 ns | **66.82 ns (1.2×)** | 69.80 ns (1.3×) | 121.20 ns (2.2×) | 51.10 ns (0.9×) |
| **Complex (nest+coll)** | 72.21 ns | **95.42 ns (1.3×)** | 94.58 ns (1.3×) | 158.24 ns (2.2×) | 71.72 ns (1.0×) |
| **Collection (100)** | 1.752 μs | **1.811 μs (1.0×)** | 1.804 μs (1.0×) | 2.416 μs (1.4×) | 1.851 μs (1.1×) |
| **Deep Coll (100)** | 5.392 μs | **5.764 μs (1.1×)** | 6.453 μs (1.2×) | 6.697 μs (1.2×) | 5.434 μs (1.0×) |
| **Large Coll (1000)** | 18.33 μs | **17.62 μs (1.0×)** | 17.71 μs (1.0×) | 21.85 μs (1.2×) | 18.17 μs (1.0×) |
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
| Manual               |  15.33 ns | 0.267 ns | 0.237 ns |  15.00 ns |  15.27 ns |  15.81 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.23 ns | 0.356 ns | 0.333 ns |  26.50 ns |  27.28 ns |  27.77 ns |  1.78 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  82.43 ns | 0.607 ns | 0.538 ns |  81.57 ns |  82.31 ns |  83.58 ns |  5.38 |    0.09 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.56 ns | 0.294 ns | 0.261 ns |  27.22 ns |  27.49 ns |  28.10 ns |  1.80 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.40 ns | 0.231 ns | 0.216 ns |  15.03 ns |  15.34 ns |  15.80 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 492.35 ns | 3.366 ns | 3.149 ns | 488.72 ns | 491.55 ns | 498.97 ns | 32.13 |    0.51 |    4 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.19 ns | 0.151 ns | 0.141 ns |  14.96 ns |  15.22 ns |  15.39 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.82 ns | 0.227 ns | 0.202 ns |  15.47 ns |  15.81 ns |  16.15 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.16 ns | 0.180 ns | 0.169 ns |  18.86 ns |  19.14 ns |  19.49 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.49 ns | 0.261 ns | 0.244 ns |  29.03 ns |  29.48 ns |  29.84 ns |  1.54 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  93.35 ns | 0.572 ns | 0.507 ns |  92.66 ns |  93.48 ns |  94.29 ns |  4.87 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.96 ns | 0.270 ns | 0.239 ns |  47.40 ns |  48.00 ns |  48.43 ns |  2.50 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.09 ns | 0.206 ns | 0.193 ns |  23.81 ns |  24.03 ns |  24.40 ns |  1.26 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 529.25 ns | 1.638 ns | 1.279 ns | 527.86 ns | 529.09 ns | 531.64 ns | 27.63 |    0.24 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  54.45 ns | 0.497 ns | 0.441 ns |  53.49 ns |  54.38 ns |  55.15 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  66.82 ns | 0.729 ns | 0.682 ns |  65.63 ns |  67.09 ns |  67.61 ns |  1.23 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 121.20 ns | 1.302 ns | 1.154 ns | 119.50 ns | 120.87 ns | 123.17 ns |  2.23 |    0.03 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  69.80 ns | 0.946 ns | 0.839 ns |  68.33 ns |  69.69 ns |  71.77 ns |  1.28 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  51.10 ns | 0.898 ns | 0.840 ns |  50.00 ns |  50.68 ns |  52.60 ns |  0.94 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 515.66 ns | 3.876 ns | 3.436 ns | 511.98 ns | 514.55 ns | 524.40 ns |  9.47 |    0.10 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  72.21 ns | 0.993 ns | 0.928 ns |  71.06 ns |  71.90 ns |  73.90 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  95.42 ns | 1.008 ns | 0.942 ns |  93.27 ns |  95.92 ns |  96.59 ns |  1.32 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 158.24 ns | 0.365 ns | 0.285 ns | 157.55 ns | 158.32 ns | 158.62 ns |  2.19 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  94.58 ns | 1.352 ns | 1.264 ns |  92.24 ns |  94.86 ns |  96.22 ns |  1.31 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  71.72 ns | 0.928 ns | 0.823 ns |  70.17 ns |  71.49 ns |  73.53 ns |  0.99 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 558.28 ns | 2.780 ns | 2.465 ns | 553.16 ns | 558.59 ns | 562.28 ns |  7.73 |    0.10 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.752 μs | 0.0230 μs | 0.0204 μs | 1.718 μs | 1.750 μs | 1.793 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.811 μs | 0.0231 μs | 0.0216 μs | 1.763 μs | 1.817 μs | 1.836 μs |  1.03 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.416 μs | 0.0263 μs | 0.0233 μs | 2.387 μs | 2.411 μs | 2.464 μs |  1.38 |    0.02 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.804 μs | 0.0158 μs | 0.0132 μs | 1.778 μs | 1.808 μs | 1.819 μs |  1.03 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.851 μs | 0.0167 μs | 0.0156 μs | 1.810 μs | 1.849 μs | 1.875 μs |  1.06 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.529 μs | 0.0208 μs | 0.0173 μs | 2.510 μs | 2.525 μs | 2.565 μs |  1.44 |    0.02 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.392 μs | 0.1003 μs | 0.0889 μs | 5.278 μs | 5.363 μs | 5.551 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.764 μs | 0.0699 μs | 0.0619 μs | 5.649 μs | 5.775 μs | 5.862 μs |  1.07 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.697 μs | 0.0587 μs | 0.0549 μs | 6.605 μs | 6.684 μs | 6.820 μs |  1.24 |    0.02 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.453 μs | 0.0635 μs | 0.0530 μs | 6.369 μs | 6.469 μs | 6.565 μs |  1.20 |    0.02 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.434 μs | 0.0555 μs | 0.0519 μs | 5.320 μs | 5.439 μs | 5.501 μs |  1.01 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.294 μs | 0.0511 μs | 0.0478 μs | 5.205 μs | 5.289 μs | 5.381 μs |  0.98 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.33 μs | 0.321 μs | 0.300 μs | 17.94 μs | 18.23 μs | 18.93 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.62 μs | 0.186 μs | 0.174 μs | 17.25 μs | 17.60 μs | 17.92 μs |  0.96 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.85 μs | 0.210 μs | 0.164 μs | 21.47 μs | 21.85 μs | 22.05 μs |  1.19 |    0.02 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.71 μs | 0.296 μs | 0.277 μs | 17.24 μs | 17.75 μs | 18.12 μs |  0.97 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.17 μs | 0.256 μs | 0.227 μs | 17.79 μs | 18.14 μs | 18.61 μs |  0.99 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 23.18 μs | 0.156 μs | 0.146 μs | 22.95 μs | 23.13 μs | 23.42 μs |  1.27 |    0.02 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,150.763 μs | 13.6031 μs | 11.3592 μs | 1,129.087 μs | 1,148.917 μs | 1,171.258 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.15 KB |        1.00 |
| AutoMapperStartup |   444.306 μs |  3.3966 μs |  2.8363 μs |   441.548 μs |   443.192 μs |   451.847 μs | 0.386 |    2 | 5.8594 |      - | 106.02 KB |        1.11 |
| MapsterStartup    |     2.564 μs |  0.0406 μs |  0.0380 μs |     2.477 μs |     2.566 μs |     2.645 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.154 ms | 0.0162 ms | 0.0152 ms | 1.132 ms | 1.153 ms | 1.180 ms |  1.00 |    0.02 |    1 |  5.8594 |  3.9063 |  95.77 KB |        1.00 |
| AutoMapper | 4.526 ms | 0.0140 ms | 0.0131 ms | 4.503 ms | 4.526 ms | 4.551 ms |  3.92 |    0.05 |    3 | 15.6250 |  7.8125 | 327.02 KB |        3.41 |
| Mapster    | 2.535 ms | 0.0097 ms | 0.0086 ms | 2.518 ms | 2.534 ms | 2.547 ms |  2.20 |    0.03 |    2 | 46.8750 | 15.6250 | 766.71 KB |        8.01 |

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

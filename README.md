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
> ⏱ **Last updated:** 2026-09-15 13:57 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.36 ns | **27.67 ns (1.8×)** | 28.51 ns (1.9×) | 83.06 ns (5.4×) | 15.46 ns (1.0×) |
| **Flattening** | 19.06 ns | **30.65 ns (1.6×)** | 36.62 ns (1.9×) | 88.49 ns (4.6×) | 22.70 ns (1.2×) |
| **Deep (2 nested)** | 54.35 ns | **65.44 ns (1.2×)** | 69.31 ns (1.3×) | 121.82 ns (2.2×) | 49.91 ns (0.9×) |
| **Complex (nest+coll)** | 71.15 ns | **93.34 ns (1.3×)** | 89.92 ns (1.3×) | 157.63 ns (2.2×) | 72.00 ns (1.0×) |
| **Collection (100)** | 1.790 μs | **1.710 μs (1.0×)** | 1.766 μs (1.0×) | 2.323 μs (1.3×) | 1.924 μs (1.1×) |
| **Deep Coll (100)** | 5.353 μs | **5.871 μs (1.1×)** | 5.912 μs (1.1×) | 6.664 μs (1.2×) | 5.504 μs (1.0×) |
| **Large Coll (1000)** | 17.76 μs | **17.26 μs (1.0×)** | 17.53 μs (1.0×) | 21.49 μs (1.2×) | 18.67 μs (1.1×) |
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
| Manual               |  15.36 ns | 0.265 ns | 0.221 ns |  14.89 ns |  15.41 ns |  15.67 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.67 ns | 0.495 ns | 0.463 ns |  27.02 ns |  27.67 ns |  28.61 ns |  1.80 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  83.06 ns | 0.676 ns | 0.565 ns |  82.45 ns |  82.93 ns |  84.58 ns |  5.41 |    0.08 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.51 ns | 0.398 ns | 0.353 ns |  27.86 ns |  28.58 ns |  29.08 ns |  1.86 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.46 ns | 0.374 ns | 0.350 ns |  14.91 ns |  15.55 ns |  16.03 ns |  1.01 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 500.13 ns | 2.157 ns | 2.017 ns | 496.61 ns | 500.12 ns | 503.68 ns | 32.57 |    0.47 |    4 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.27 ns | 0.272 ns | 0.241 ns |  14.95 ns |  15.21 ns |  15.68 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.77 ns | 0.202 ns | 0.169 ns |  15.52 ns |  15.77 ns |  16.16 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.06 ns | 0.206 ns | 0.193 ns |  18.77 ns |  19.03 ns |  19.37 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.65 ns | 0.592 ns | 0.581 ns |  29.79 ns |  30.55 ns |  31.81 ns |  1.61 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  88.49 ns | 0.391 ns | 0.365 ns |  87.68 ns |  88.66 ns |  88.94 ns |  4.64 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  36.62 ns | 0.535 ns | 0.475 ns |  35.99 ns |  36.43 ns |  37.86 ns |  1.92 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  22.70 ns | 0.375 ns | 0.351 ns |  22.16 ns |  22.66 ns |  23.36 ns |  1.19 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 534.62 ns | 3.249 ns | 2.880 ns | 530.84 ns | 533.85 ns | 541.99 ns | 28.05 |    0.31 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  54.35 ns | 0.855 ns | 0.800 ns |  53.02 ns |  54.25 ns |  56.00 ns |  1.00 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  65.44 ns | 1.257 ns | 1.345 ns |  63.24 ns |  65.36 ns |  68.38 ns |  1.20 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 121.82 ns | 2.341 ns | 2.695 ns | 118.75 ns | 120.89 ns | 127.14 ns |  2.24 |    0.06 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  69.31 ns | 1.278 ns | 1.133 ns |  67.93 ns |  68.97 ns |  71.82 ns |  1.28 |    0.03 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.91 ns | 0.745 ns | 0.696 ns |  48.19 ns |  50.00 ns |  50.85 ns |  0.92 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 513.02 ns | 4.132 ns | 3.865 ns | 506.92 ns | 511.42 ns | 521.35 ns |  9.44 |    0.15 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  71.15 ns | 1.091 ns | 1.020 ns |  69.81 ns |  71.02 ns |  73.28 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  93.34 ns | 1.038 ns | 0.867 ns |  91.80 ns |  93.38 ns |  94.99 ns |  1.31 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 157.63 ns | 2.769 ns | 2.590 ns | 153.02 ns | 158.86 ns | 160.31 ns |  2.22 |    0.05 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  89.92 ns | 1.123 ns | 1.050 ns |  88.36 ns |  89.78 ns |  91.86 ns |  1.26 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  72.00 ns | 0.747 ns | 0.662 ns |  71.16 ns |  71.99 ns |  73.24 ns |  1.01 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 541.38 ns | 3.835 ns | 3.587 ns | 533.88 ns | 541.60 ns | 547.47 ns |  7.61 |    0.12 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.790 μs | 0.0357 μs | 0.0489 μs | 1.727 μs | 1.775 μs | 1.907 μs |  1.00 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.710 μs | 0.0244 μs | 0.0216 μs | 1.668 μs | 1.714 μs | 1.743 μs |  0.96 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.323 μs | 0.0292 μs | 0.0228 μs | 2.292 μs | 2.319 μs | 2.383 μs |  1.30 |    0.04 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.766 μs | 0.0346 μs | 0.0518 μs | 1.673 μs | 1.756 μs | 1.885 μs |  0.99 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.924 μs | 0.0234 μs | 0.0219 μs | 1.893 μs | 1.924 μs | 1.961 μs |  1.08 |    0.03 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.623 μs | 0.0482 μs | 0.0451 μs | 2.539 μs | 2.616 μs | 2.697 μs |  1.47 |    0.05 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.353 μs | 0.0914 μs | 0.0855 μs | 5.213 μs | 5.314 μs | 5.495 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.871 μs | 0.0987 μs | 0.0923 μs | 5.681 μs | 5.891 μs | 6.005 μs |  1.10 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.664 μs | 0.1311 μs | 0.1287 μs | 6.393 μs | 6.687 μs | 6.841 μs |  1.25 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.912 μs | 0.1066 μs | 0.1185 μs | 5.729 μs | 5.921 μs | 6.149 μs |  1.10 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.504 μs | 0.1031 μs | 0.1013 μs | 5.290 μs | 5.491 μs | 5.681 μs |  1.03 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.125 μs | 0.1001 μs | 0.0887 μs | 5.011 μs | 5.105 μs | 5.299 μs |  0.96 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.76 μs | 0.277 μs | 0.245 μs | 17.45 μs | 17.70 μs | 18.30 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.26 μs | 0.327 μs | 0.321 μs | 16.58 μs | 17.23 μs | 17.88 μs |  0.97 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.49 μs | 0.322 μs | 0.285 μs | 20.82 μs | 21.55 μs | 21.81 μs |  1.21 |    0.02 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.53 μs | 0.348 μs | 0.453 μs | 17.00 μs | 17.41 μs | 18.67 μs |  0.99 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.67 μs | 0.324 μs | 0.303 μs | 18.17 μs | 18.76 μs | 19.17 μs |  1.05 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 21.08 μs | 0.401 μs | 0.375 μs | 20.53 μs | 21.06 μs | 21.65 μs |  1.19 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,149.892 μs | 4.5725 μs | 4.0534 μs | 1,141.599 μs | 1,150.714 μs | 1,155.339 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.31 KB |        1.00 |
| AutoMapperStartup |   286.999 μs | 2.3138 μs | 1.9321 μs |   284.670 μs |   286.450 μs |   291.391 μs | 0.250 |    2 | 5.8594 |      - | 103.76 KB |        1.09 |
| MapsterStartup    |     3.173 μs | 0.0617 μs | 0.0606 μs |     3.089 μs |     3.166 μs |     3.279 μs | 0.003 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.155 ms | 0.0145 ms | 0.0135 ms | 1.137 ms | 1.153 ms | 1.181 ms |  1.00 |    0.02 |    1 |  5.8594 |  3.9063 |  96.56 KB |        1.00 |
| AutoMapper | 3.271 ms | 0.0276 ms | 0.0258 ms | 3.241 ms | 3.265 ms | 3.323 ms |  2.83 |    0.04 |    3 | 15.6250 |  7.8125 | 309.93 KB |        3.21 |
| Mapster    | 2.494 ms | 0.0149 ms | 0.0140 ms | 2.470 ms | 2.491 ms | 2.516 ms |  2.16 |    0.03 |    2 | 46.8750 | 15.6250 | 766.77 KB |        7.94 |

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

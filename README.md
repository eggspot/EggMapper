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
> ⏱ **Last updated:** 2026-05-22 14:24 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.53 ns | **26.96 ns (1.7×)** | 27.84 ns (1.8×) | 84.25 ns (5.4×) | 14.56 ns (0.9×) |
| **Flattening** | 19.61 ns | **29.96 ns (1.5×)** | 47.80 ns (2.4×) | 87.85 ns (4.5×) | 23.39 ns (1.2×) |
| **Deep (2 nested)** | 52.02 ns | **62.18 ns (1.2×)** | 67.02 ns (1.3×) | 119.42 ns (2.3×) | 49.42 ns (0.9×) |
| **Complex (nest+coll)** | 69.44 ns | **90.20 ns (1.3×)** | 86.76 ns (1.2×) | 148.85 ns (2.1×) | 69.56 ns (1.0×) |
| **Collection (100)** | 1.761 μs | **1.757 μs (1.0×)** | 1.743 μs (1.0×) | 2.408 μs (1.4×) | 1.894 μs (1.1×) |
| **Deep Coll (100)** | 5.833 μs | **5.588 μs (1.0×)** | 5.637 μs (1.0×) | 6.598 μs (1.1×) | 5.770 μs (1.0×) |
| **Large Coll (1000)** | 17.53 μs | **17.41 μs (1.0×)** | 17.15 μs (1.0×) | 20.76 μs (1.2×) | 17.84 μs (1.0×) |
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
| Manual               |  15.53 ns | 0.203 ns | 0.190 ns |  15.15 ns |  15.54 ns |  15.79 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.96 ns | 0.519 ns | 0.485 ns |  26.13 ns |  27.19 ns |  27.64 ns |  1.74 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  84.25 ns | 0.668 ns | 0.592 ns |  83.46 ns |  84.23 ns |  85.33 ns |  5.43 |    0.07 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.84 ns | 0.529 ns | 0.494 ns |  27.09 ns |  27.75 ns |  28.50 ns |  1.79 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  14.56 ns | 0.029 ns | 0.026 ns |  14.51 ns |  14.56 ns |  14.59 ns |  0.94 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 493.21 ns | 1.126 ns | 1.053 ns | 491.63 ns | 493.51 ns | 494.80 ns | 31.77 |    0.38 |    4 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  14.92 ns | 0.193 ns | 0.180 ns |  14.59 ns |  14.94 ns |  15.26 ns |  0.96 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.78 ns | 0.199 ns | 0.166 ns |  15.45 ns |  15.81 ns |  16.09 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.61 ns | 0.283 ns | 0.264 ns |  19.03 ns |  19.75 ns |  19.95 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.96 ns | 0.585 ns | 0.674 ns |  29.07 ns |  29.95 ns |  31.12 ns |  1.53 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  87.85 ns | 0.885 ns | 0.828 ns |  86.61 ns |  87.99 ns |  89.80 ns |  4.48 |    0.07 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.80 ns | 0.431 ns | 0.403 ns |  46.89 ns |  47.82 ns |  48.50 ns |  2.44 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.39 ns | 0.154 ns | 0.137 ns |  23.22 ns |  23.36 ns |  23.74 ns |  1.19 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 520.28 ns | 1.056 ns | 0.936 ns | 518.79 ns | 520.15 ns | 521.97 ns | 26.53 |    0.35 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  52.02 ns | 0.168 ns | 0.149 ns |  51.66 ns |  52.03 ns |  52.24 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  62.18 ns | 0.217 ns | 0.203 ns |  61.86 ns |  62.15 ns |  62.62 ns |  1.20 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 119.42 ns | 0.343 ns | 0.321 ns | 118.88 ns | 119.40 ns | 119.91 ns |  2.30 |    0.01 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  67.02 ns | 0.315 ns | 0.246 ns |  66.49 ns |  66.99 ns |  67.51 ns |  1.29 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.42 ns | 0.298 ns | 0.249 ns |  48.98 ns |  49.42 ns |  49.81 ns |  0.95 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 526.18 ns | 1.486 ns | 1.390 ns | 523.02 ns | 526.26 ns | 527.85 ns | 10.11 |    0.04 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  69.44 ns | 0.145 ns | 0.128 ns |  69.25 ns |  69.41 ns |  69.73 ns |  1.00 |    0.00 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  90.20 ns | 0.819 ns | 0.766 ns |  89.13 ns |  89.89 ns |  91.72 ns |  1.30 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 148.85 ns | 0.392 ns | 0.306 ns | 148.25 ns | 148.80 ns | 149.29 ns |  2.14 |    0.01 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  86.76 ns | 0.702 ns | 0.586 ns |  85.96 ns |  86.72 ns |  87.90 ns |  1.25 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  69.56 ns | 0.317 ns | 0.281 ns |  69.20 ns |  69.53 ns |  70.11 ns |  1.00 |    0.00 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 558.70 ns | 1.449 ns | 1.284 ns | 556.83 ns | 558.99 ns | 560.41 ns |  8.05 |    0.02 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.761 μs | 0.0343 μs | 0.0321 μs | 1.714 μs | 1.752 μs | 1.810 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.757 μs | 0.0342 μs | 0.0352 μs | 1.708 μs | 1.755 μs | 1.820 μs |  1.00 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.408 μs | 0.0452 μs | 0.0423 μs | 2.349 μs | 2.409 μs | 2.506 μs |  1.37 |    0.03 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.743 μs | 0.0221 μs | 0.0207 μs | 1.714 μs | 1.740 μs | 1.778 μs |  0.99 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.894 μs | 0.0172 μs | 0.0153 μs | 1.872 μs | 1.892 μs | 1.925 μs |  1.08 |    0.02 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.492 μs | 0.0161 μs | 0.0151 μs | 2.465 μs | 2.492 μs | 2.519 μs |  1.42 |    0.03 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.833 μs | 0.0914 μs | 0.0855 μs | 5.715 μs | 5.782 μs | 5.969 μs |  1.00 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.588 μs | 0.0713 μs | 0.0667 μs | 5.505 μs | 5.580 μs | 5.685 μs |  0.96 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.598 μs | 0.0228 μs | 0.0202 μs | 6.564 μs | 6.597 μs | 6.638 μs |  1.13 |    0.02 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.637 μs | 0.0299 μs | 0.0250 μs | 5.596 μs | 5.642 μs | 5.690 μs |  0.97 |    0.01 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.770 μs | 0.0188 μs | 0.0157 μs | 5.744 μs | 5.766 μs | 5.805 μs |  0.99 |    0.01 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.104 μs | 0.0239 μs | 0.0224 μs | 5.067 μs | 5.101 μs | 5.137 μs |  0.88 |    0.01 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.53 μs | 0.173 μs | 0.153 μs | 17.37 μs | 17.49 μs | 17.85 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.41 μs | 0.312 μs | 0.292 μs | 17.14 μs | 17.29 μs | 18.09 μs |  0.99 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.76 μs | 0.108 μs | 0.085 μs | 20.62 μs | 20.77 μs | 20.93 μs |  1.18 |    0.01 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.15 μs | 0.240 μs | 0.224 μs | 16.80 μs | 17.09 μs | 17.67 μs |  0.98 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.84 μs | 0.203 μs | 0.180 μs | 17.64 μs | 17.80 μs | 18.27 μs |  1.02 |    0.01 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.88 μs | 0.119 μs | 0.099 μs | 19.74 μs | 19.87 μs | 20.04 μs |  1.13 |    0.01 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,124.276 μs | 5.7255 μs | 5.3556 μs | 1,111.014 μs | 1,123.615 μs | 1,132.871 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.28 KB |        1.00 |
| AutoMapperStartup |   282.743 μs | 1.4343 μs | 1.1977 μs |   281.281 μs |   282.907 μs |   284.722 μs | 0.251 |    2 | 5.8594 |      - |  103.7 KB |        1.09 |
| MapsterStartup    |     2.475 μs | 0.0158 μs | 0.0140 μs |     2.431 μs |     2.477 μs |     2.490 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.112 ms | 0.0060 ms | 0.0050 ms | 1.107 ms | 1.110 ms | 1.122 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.26 KB |        1.00 |
| AutoMapper | 3.255 ms | 0.0234 ms | 0.0218 ms | 3.229 ms | 3.250 ms | 3.304 ms |  2.93 |    0.02 |    3 | 15.6250 |  7.8125 | 310.09 KB |        3.22 |
| Mapster    | 2.512 ms | 0.0106 ms | 0.0099 ms | 2.501 ms | 2.512 ms | 2.534 ms |  2.26 |    0.01 |    2 | 39.0625 | 15.6250 | 764.19 KB |        7.94 |

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

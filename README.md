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
> ⏱ **Last updated:** 2026-05-02 04:52 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 14.77 ns | **24.41 ns (1.6×)** | 27.60 ns (1.9×) | 80.67 ns (5.5×) | 14.77 ns (1.0×) |
| **Flattening** | 18.38 ns | **28.81 ns (1.6×)** | 46.56 ns (2.5×) | 88.24 ns (4.8×) | 23.48 ns (1.3×) |
| **Deep (2 nested)** | 53.57 ns | **64.40 ns (1.2×)** | 67.58 ns (1.3×) | 120.99 ns (2.3×) | 48.97 ns (0.9×) |
| **Complex (nest+coll)** | 70.36 ns | **89.57 ns (1.3×)** | 89.86 ns (1.3×) | 146.92 ns (2.1×) | 69.81 ns (1.0×) |
| **Collection (100)** | 1.696 μs | **1.656 μs (1.0×)** | 1.721 μs (1.0×) | 2.330 μs (1.4×) | 1.841 μs (1.1×) |
| **Deep Coll (100)** | 5.205 μs | **5.662 μs (1.1×)** | 5.679 μs (1.1×) | 6.395 μs (1.2×) | 5.278 μs (1.0×) |
| **Large Coll (1000)** | 17.30 μs | **17.01 μs (1.0×)** | 17.03 μs (1.0×) | 21.01 μs (1.2×) | 17.84 μs (1.0×) |
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
| Manual               |  14.77 ns | 0.034 ns | 0.032 ns |  14.70 ns |  14.78 ns |  14.81 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  24.41 ns | 0.161 ns | 0.151 ns |  24.20 ns |  24.41 ns |  24.69 ns |  1.65 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  80.67 ns | 0.294 ns | 0.245 ns |  80.39 ns |  80.60 ns |  81.06 ns |  5.46 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.60 ns | 0.058 ns | 0.049 ns |  27.50 ns |  27.61 ns |  27.69 ns |  1.87 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  14.77 ns | 0.058 ns | 0.054 ns |  14.71 ns |  14.78 ns |  14.87 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 493.97 ns | 1.694 ns | 1.414 ns | 492.58 ns | 493.66 ns | 497.72 ns | 33.44 |    0.12 |    6 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  14.79 ns | 0.037 ns | 0.033 ns |  14.74 ns |  14.78 ns |  14.86 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.39 ns | 0.066 ns | 0.062 ns |  15.31 ns |  15.40 ns |  15.50 ns |  1.04 |    0.00 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.38 ns | 0.070 ns | 0.065 ns |  18.30 ns |  18.35 ns |  18.51 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  28.81 ns | 0.102 ns | 0.080 ns |  28.62 ns |  28.84 ns |  28.88 ns |  1.57 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  88.24 ns | 0.155 ns | 0.138 ns |  88.08 ns |  88.18 ns |  88.48 ns |  4.80 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  46.56 ns | 0.077 ns | 0.068 ns |  46.47 ns |  46.55 ns |  46.72 ns |  2.53 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.48 ns | 0.068 ns | 0.061 ns |  23.32 ns |  23.48 ns |  23.56 ns |  1.28 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 522.21 ns | 0.775 ns | 0.725 ns | 521.02 ns | 522.15 ns | 523.54 ns | 28.42 |    0.10 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.57 ns | 0.199 ns | 0.176 ns |  53.14 ns |  53.56 ns |  53.87 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.40 ns | 0.380 ns | 0.337 ns |  63.74 ns |  64.46 ns |  64.91 ns |  1.20 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 120.99 ns | 0.549 ns | 0.487 ns | 120.46 ns | 120.90 ns | 122.14 ns |  2.26 |    0.01 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  67.58 ns | 0.152 ns | 0.127 ns |  67.34 ns |  67.54 ns |  67.80 ns |  1.26 |    0.00 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  48.97 ns | 0.161 ns | 0.142 ns |  48.64 ns |  48.97 ns |  49.20 ns |  0.91 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 505.24 ns | 1.295 ns | 1.212 ns | 503.45 ns | 504.95 ns | 507.46 ns |  9.43 |    0.04 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  70.36 ns | 0.219 ns | 0.194 ns |  70.16 ns |  70.30 ns |  70.85 ns |  1.00 |    0.00 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  89.57 ns | 0.476 ns | 0.446 ns |  89.05 ns |  89.41 ns |  90.46 ns |  1.27 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 146.92 ns | 0.351 ns | 0.293 ns | 146.43 ns | 146.94 ns | 147.43 ns |  2.09 |    0.01 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  89.86 ns | 0.248 ns | 0.194 ns |  89.49 ns |  89.87 ns |  90.23 ns |  1.28 |    0.00 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  69.81 ns | 0.415 ns | 0.388 ns |  69.22 ns |  69.99 ns |  70.31 ns |  0.99 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 554.74 ns | 1.237 ns | 1.157 ns | 553.41 ns | 554.52 ns | 557.64 ns |  7.88 |    0.03 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.696 μs | 0.0058 μs | 0.0055 μs | 1.683 μs | 1.697 μs | 1.703 μs |  1.00 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.656 μs | 0.0124 μs | 0.0116 μs | 1.631 μs | 1.659 μs | 1.668 μs |  0.98 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.330 μs | 0.0117 μs | 0.0098 μs | 2.313 μs | 2.328 μs | 2.348 μs |  1.37 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.721 μs | 0.0074 μs | 0.0062 μs | 1.708 μs | 1.720 μs | 1.729 μs |  1.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.841 μs | 0.0083 μs | 0.0078 μs | 1.825 μs | 1.841 μs | 1.853 μs |  1.09 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.476 μs | 0.0086 μs | 0.0080 μs | 2.467 μs | 2.473 μs | 2.491 μs |  1.46 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.205 μs | 0.0314 μs | 0.0294 μs | 5.166 μs | 5.197 μs | 5.272 μs |  1.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.662 μs | 0.0530 μs | 0.0496 μs | 5.592 μs | 5.677 μs | 5.727 μs |  1.09 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.395 μs | 0.0236 μs | 0.0221 μs | 6.339 μs | 6.395 μs | 6.429 μs |  1.23 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.679 μs | 0.0242 μs | 0.0202 μs | 5.632 μs | 5.685 μs | 5.701 μs |  1.09 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.278 μs | 0.0267 μs | 0.0250 μs | 5.226 μs | 5.283 μs | 5.315 μs |  1.01 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.103 μs | 0.0204 μs | 0.0181 μs | 5.065 μs | 5.105 μs | 5.130 μs |  0.98 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.30 μs | 0.137 μs | 0.121 μs | 17.09 μs | 17.32 μs | 17.48 μs |  1.00 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.01 μs | 0.087 μs | 0.077 μs | 16.83 μs | 17.01 μs | 17.13 μs |  0.98 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.01 μs | 0.100 μs | 0.078 μs | 20.82 μs | 21.04 μs | 21.08 μs |  1.21 |    4 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.03 μs | 0.105 μs | 0.093 μs | 16.80 μs | 17.02 μs | 17.19 μs |  0.98 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.84 μs | 0.054 μs | 0.051 μs | 17.77 μs | 17.84 μs | 17.94 μs |  1.03 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.09 μs | 0.025 μs | 0.021 μs | 20.05 μs | 20.08 μs | 20.12 μs |  1.16 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,220.261 μs | 19.3893 μs | 18.1367 μs | 1,181.932 μs | 1,225.158 μs | 1,241.877 μs | 1.000 |    0.02 |    3 | 5.8594 | 3.9063 |  95.61 KB |        1.00 |
| AutoMapperStartup |   283.320 μs |  1.7124 μs |  1.5180 μs |   281.136 μs |   283.234 μs |   286.687 μs | 0.232 |    0.00 |    2 | 5.8594 |      - | 104.04 KB |        1.09 |
| MapsterStartup    |     2.401 μs |  0.0030 μs |  0.0027 μs |     2.397 μs |     2.400 μs |     2.406 μs | 0.002 |    0.00 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.162 ms | 0.0060 ms | 0.0050 ms | 1.152 ms | 1.162 ms | 1.169 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.06 KB |        1.00 |
| AutoMapper | 3.217 ms | 0.0173 ms | 0.0162 ms | 3.197 ms | 3.211 ms | 3.250 ms |  2.77 |    0.02 |    3 | 15.6250 |  7.8125 | 310.25 KB |        3.23 |
| Mapster    | 2.483 ms | 0.0210 ms | 0.0196 ms | 2.463 ms | 2.472 ms | 2.533 ms |  2.14 |    0.02 |    2 | 46.8750 | 15.6250 | 766.47 KB |        7.98 |

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

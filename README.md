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
> ⏱ **Last updated:** 2026-05-01 14:40 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.48 ns | **27.48 ns (1.7×)** | 29.48 ns (1.8×) | 78.76 ns (4.8×) | 16.83 ns (1.0×) |
| **Flattening** | 19.93 ns | **31.18 ns (1.6×)** | 35.26 ns (1.8×) | 84.13 ns (4.2×) | 25.61 ns (1.3×) |
| **Deep (2 nested)** | 58.55 ns | **70.54 ns (1.2×)** | 75.32 ns (1.3×) | 111.07 ns (1.9×) | 54.55 ns (0.9×) |
| **Complex (nest+coll)** | 80.24 ns | **103.57 ns (1.3×)** | 105.36 ns (1.3×) | 153.29 ns (1.9×) | 80.57 ns (1.0×) |
| **Collection (100)** | 2.084 μs | **2.195 μs (1.1×)** | 2.184 μs (1.1×) | 2.633 μs (1.3×) | 2.286 μs (1.1×) |
| **Deep Coll (100)** | 6.742 μs | **6.585 μs (1.0×)** | 6.556 μs (1.0×) | 7.404 μs (1.1×) | 6.105 μs (0.9×) |
| **Large Coll (1000)** | 20.97 μs | **21.51 μs (1.0×)** | 21.24 μs (1.0×) | 25.62 μs (1.2×) | 21.54 μs (1.0×) |
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
| Manual               |  16.48 ns | 0.311 ns | 0.291 ns |  16.12 ns |  16.38 ns |  17.13 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.48 ns | 0.186 ns | 0.165 ns |  27.17 ns |  27.48 ns |  27.72 ns |  1.67 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  78.76 ns | 0.169 ns | 0.150 ns |  78.39 ns |  78.81 ns |  78.92 ns |  4.78 |    0.08 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.48 ns | 0.416 ns | 0.389 ns |  28.96 ns |  29.44 ns |  30.03 ns |  1.79 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.83 ns | 0.242 ns | 0.214 ns |  16.26 ns |  16.84 ns |  17.08 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 344.73 ns | 2.664 ns | 2.492 ns | 340.06 ns | 345.15 ns | 348.35 ns | 20.92 |    0.38 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  16.05 ns | 0.147 ns | 0.130 ns |  15.89 ns |  16.02 ns |  16.29 ns |  0.97 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  17.54 ns | 0.201 ns | 0.188 ns |  17.21 ns |  17.52 ns |  17.89 ns |  1.06 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.93 ns | 0.199 ns | 0.186 ns |  19.69 ns |  19.86 ns |  20.34 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.18 ns | 0.414 ns | 0.388 ns |  30.60 ns |  31.19 ns |  31.89 ns |  1.56 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  84.13 ns | 0.143 ns | 0.120 ns |  83.95 ns |  84.13 ns |  84.35 ns |  4.22 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.26 ns | 0.253 ns | 0.237 ns |  35.00 ns |  35.21 ns |  35.85 ns |  1.77 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  25.61 ns | 0.213 ns | 0.199 ns |  25.38 ns |  25.56 ns |  26.02 ns |  1.28 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 322.79 ns | 2.652 ns | 2.351 ns | 319.30 ns | 322.86 ns | 327.02 ns | 16.19 |    0.19 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  58.55 ns | 0.097 ns | 0.076 ns |  58.39 ns |  58.56 ns |  58.64 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  70.54 ns | 0.183 ns | 0.171 ns |  70.17 ns |  70.56 ns |  70.80 ns |  1.20 |    0.00 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 111.07 ns | 0.365 ns | 0.341 ns | 110.66 ns | 110.98 ns | 111.87 ns |  1.90 |    0.01 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  75.32 ns | 0.899 ns | 0.841 ns |  73.99 ns |  75.15 ns |  76.56 ns |  1.29 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  54.55 ns | 0.259 ns | 0.242 ns |  54.15 ns |  54.56 ns |  54.99 ns |  0.93 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 357.31 ns | 1.534 ns | 1.435 ns | 355.41 ns | 356.87 ns | 359.88 ns |  6.10 |    0.02 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  80.24 ns | 1.355 ns | 1.268 ns |  78.38 ns |  79.81 ns |  82.65 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 103.57 ns | 1.732 ns | 1.621 ns | 101.32 ns | 103.15 ns | 106.06 ns |  1.29 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 153.29 ns | 1.291 ns | 1.208 ns | 150.63 ns | 153.40 ns | 155.76 ns |  1.91 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     | 105.36 ns | 1.034 ns | 0.967 ns | 103.89 ns | 105.48 ns | 107.21 ns |  1.31 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  80.57 ns | 0.491 ns | 0.460 ns |  79.89 ns |  80.67 ns |  81.28 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 408.87 ns | 2.273 ns | 2.126 ns | 405.01 ns | 409.06 ns | 413.26 ns |  5.10 |    0.08 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.084 μs | 0.0410 μs | 0.0663 μs | 1.948 μs | 2.086 μs | 2.228 μs |  1.00 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.195 μs | 0.0284 μs | 0.0266 μs | 2.156 μs | 2.199 μs | 2.248 μs |  1.05 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.633 μs | 0.0504 μs | 0.0518 μs | 2.546 μs | 2.635 μs | 2.733 μs |  1.27 |    0.05 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.184 μs | 0.0436 μs | 0.0386 μs | 2.117 μs | 2.177 μs | 2.263 μs |  1.05 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.286 μs | 0.0380 μs | 0.0355 μs | 2.223 μs | 2.280 μs | 2.343 μs |  1.10 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.810 μs | 0.0551 μs | 0.0790 μs | 2.649 μs | 2.791 μs | 2.952 μs |  1.35 |    0.06 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.742 μs | 0.1264 μs | 0.1405 μs | 6.493 μs | 6.734 μs | 6.972 μs |  1.00 |    0.03 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.585 μs | 0.1281 μs | 0.2104 μs | 6.331 μs | 6.512 μs | 7.016 μs |  0.98 |    0.04 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.404 μs | 0.1127 μs | 0.1054 μs | 7.254 μs | 7.392 μs | 7.603 μs |  1.10 |    0.03 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.556 μs | 0.0455 μs | 0.0403 μs | 6.470 μs | 6.554 μs | 6.619 μs |  0.97 |    0.02 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.105 μs | 0.0807 μs | 0.0755 μs | 6.003 μs | 6.100 μs | 6.247 μs |  0.91 |    0.02 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.714 μs | 0.0355 μs | 0.0332 μs | 5.662 μs | 5.711 μs | 5.780 μs |  0.85 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 20.97 μs | 0.414 μs | 0.756 μs | 19.62 μs | 20.70 μs | 22.41 μs |  1.00 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 21.51 μs | 0.346 μs | 0.323 μs | 20.78 μs | 21.49 μs | 22.02 μs |  1.03 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 25.62 μs | 0.508 μs | 1.027 μs | 23.94 μs | 25.55 μs | 27.53 μs |  1.22 |    0.07 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 21.24 μs | 0.394 μs | 0.368 μs | 20.58 μs | 21.33 μs | 21.90 μs |  1.01 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.54 μs | 0.377 μs | 0.315 μs | 20.54 μs | 21.60 μs | 21.82 μs |  1.03 |    0.04 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 22.89 μs | 0.453 μs | 0.590 μs | 21.86 μs | 22.97 μs | 23.64 μs |  1.09 |    0.05 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Median       | Min          | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,213.265 μs | 3.1895 μs | 2.9834 μs | 1,213.510 μs | 1,206.685 μs | 1,216.429 μs | 1.000 |    3 | 5.8594 |      - |  95.64 KB |        1.00 |
| AutoMapperStartup |   242.100 μs | 1.2821 μs | 1.1365 μs |   242.075 μs |   239.723 μs |   244.002 μs | 0.200 |    2 | 5.8594 |      - | 104.15 KB |        1.09 |
| MapsterStartup    |     2.654 μs | 0.0521 μs | 0.0730 μs |     2.614 μs |     2.563 μs |     2.777 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.179 ms | 0.0087 ms | 0.0077 ms | 1.167 ms | 1.179 ms | 1.198 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.94 KB |        1.00 |
| AutoMapper | 3.453 ms | 0.0268 ms | 0.0251 ms | 3.416 ms | 3.453 ms | 3.497 ms |  2.93 |    0.03 |    3 | 15.6250 |  7.8125 | 309.93 KB |        3.20 |
| Mapster    | 2.574 ms | 0.0103 ms | 0.0091 ms | 2.558 ms | 2.573 ms | 2.594 ms |  2.18 |    0.02 |    2 | 46.8750 | 15.6250 | 766.48 KB |        7.91 |

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

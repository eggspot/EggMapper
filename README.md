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
> ⏱ **Last updated:** 2026-05-01 17:17 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 17.48 ns | **29.10 ns (1.7×)** | 29.58 ns (1.7×) | 81.29 ns (4.7×) | 16.84 ns (1.0×) |
| **Flattening** | 20.78 ns | **33.35 ns (1.6×)** | 35.07 ns (1.7×) | 82.03 ns (4.0×) | 26.51 ns (1.3×) |
| **Deep (2 nested)** | 61.45 ns | **71.09 ns (1.2×)** | 76.83 ns (1.2×) | 112.12 ns (1.8×) | 56.80 ns (0.9×) |
| **Complex (nest+coll)** | 82.27 ns | **110.53 ns (1.3×)** | 104.78 ns (1.3×) | 149.25 ns (1.8×) | 81.82 ns (1.0×) |
| **Collection (100)** | 2.117 μs | **2.166 μs (1.0×)** | 2.229 μs (1.1×) | 2.804 μs (1.3×) | 2.234 μs (1.1×) |
| **Deep Coll (100)** | 6.562 μs | **6.796 μs (1.0×)** | 8.017 μs (1.2×) | 8.431 μs (1.3×) | 6.876 μs (1.1×) |
| **Large Coll (1000)** | 19.76 μs | **20.53 μs (1.0×)** | 19.71 μs (1.0×) | 23.52 μs (1.2×) | 21.30 μs (1.1×) |
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
| Manual               |  17.48 ns | 0.290 ns | 0.271 ns |  17.20 ns |  17.33 ns |  17.92 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  29.10 ns | 0.234 ns | 0.219 ns |  28.65 ns |  29.13 ns |  29.41 ns |  1.67 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  81.29 ns | 0.150 ns | 0.140 ns |  80.84 ns |  81.33 ns |  81.44 ns |  4.65 |    0.07 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.58 ns | 0.405 ns | 0.379 ns |  28.95 ns |  29.63 ns |  30.35 ns |  1.69 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.84 ns | 0.281 ns | 0.263 ns |  16.16 ns |  16.90 ns |  17.15 ns |  0.96 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 340.41 ns | 1.091 ns | 0.967 ns | 338.53 ns | 340.77 ns | 341.96 ns | 19.48 |    0.29 |    4 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  17.30 ns | 0.294 ns | 0.275 ns |  16.80 ns |  17.25 ns |  17.70 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  18.13 ns | 0.245 ns | 0.229 ns |  17.66 ns |  18.21 ns |  18.51 ns |  1.04 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.78 ns | 0.335 ns | 0.314 ns |  20.11 ns |  20.81 ns |  21.30 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  33.35 ns | 0.325 ns | 0.304 ns |  32.90 ns |  33.28 ns |  33.99 ns |  1.61 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  82.03 ns | 0.123 ns | 0.109 ns |  81.84 ns |  82.03 ns |  82.17 ns |  3.95 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.07 ns | 0.147 ns | 0.115 ns |  34.94 ns |  35.04 ns |  35.37 ns |  1.69 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  26.51 ns | 0.256 ns | 0.240 ns |  26.15 ns |  26.57 ns |  26.82 ns |  1.28 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 331.45 ns | 1.684 ns | 1.575 ns | 328.56 ns | 331.74 ns | 333.14 ns | 15.96 |    0.25 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  61.45 ns | 0.462 ns | 0.432 ns |  60.82 ns |  61.31 ns |  62.21 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  71.09 ns | 0.783 ns | 0.733 ns |  70.12 ns |  70.97 ns |  72.38 ns |  1.16 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 112.12 ns | 1.264 ns | 1.182 ns | 110.39 ns | 112.12 ns | 114.71 ns |  1.82 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  76.83 ns | 1.184 ns | 1.108 ns |  74.72 ns |  77.27 ns |  78.53 ns |  1.25 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  56.80 ns | 0.477 ns | 0.446 ns |  55.85 ns |  56.80 ns |  57.40 ns |  0.92 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 367.61 ns | 0.795 ns | 0.743 ns | 366.63 ns | 367.63 ns | 368.89 ns |  5.98 |    0.04 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  82.27 ns | 1.125 ns | 1.053 ns |  79.35 ns |  82.91 ns |  83.17 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 110.53 ns | 1.795 ns | 1.679 ns | 107.35 ns | 110.28 ns | 113.35 ns |  1.34 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 149.25 ns | 1.046 ns | 0.978 ns | 146.93 ns | 149.42 ns | 150.62 ns |  1.81 |    0.03 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     | 104.78 ns | 0.950 ns | 0.793 ns | 103.53 ns | 104.61 ns | 106.74 ns |  1.27 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  81.82 ns | 1.484 ns | 1.388 ns |  79.36 ns |  82.05 ns |  83.45 ns |  0.99 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 415.56 ns | 4.699 ns | 4.395 ns | 408.10 ns | 417.33 ns | 420.83 ns |  5.05 |    0.08 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.117 μs | 0.0413 μs | 0.1058 μs | 1.937 μs | 2.101 μs | 2.381 μs |  1.00 |    0.07 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.166 μs | 0.0431 μs | 0.0683 μs | 2.059 μs | 2.155 μs | 2.372 μs |  1.03 |    0.06 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.804 μs | 0.0517 μs | 0.0458 μs | 2.720 μs | 2.811 μs | 2.899 μs |  1.33 |    0.07 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.229 μs | 0.0438 μs | 0.0521 μs | 2.068 μs | 2.236 μs | 2.297 μs |  1.06 |    0.06 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.234 μs | 0.0443 μs | 0.0843 μs | 2.097 μs | 2.218 μs | 2.416 μs |  1.06 |    0.07 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.879 μs | 0.0571 μs | 0.1336 μs | 2.613 μs | 2.838 μs | 3.176 μs |  1.36 |    0.09 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.562 μs | 0.1300 μs | 0.3380 μs | 5.929 μs | 6.593 μs | 7.121 μs |  1.00 |    0.07 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.796 μs | 0.1303 μs | 0.1694 μs | 6.599 μs | 6.735 μs | 7.157 μs |  1.04 |    0.06 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 8.431 μs | 0.1659 μs | 0.2772 μs | 7.950 μs | 8.437 μs | 8.946 μs |  1.29 |    0.08 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 8.017 μs | 0.1532 μs | 0.1639 μs | 7.602 μs | 7.990 μs | 8.233 μs |  1.22 |    0.07 |    2 | 1.6632 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.876 μs | 0.0800 μs | 0.0748 μs | 6.714 μs | 6.889 μs | 7.001 μs |  1.05 |    0.06 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 6.443 μs | 0.1068 μs | 0.0999 μs | 6.299 μs | 6.481 μs | 6.586 μs |  0.98 |    0.05 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.76 μs | 0.359 μs | 0.537 μs | 19.26 μs | 19.53 μs | 21.00 μs |  1.00 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 20.53 μs | 0.402 μs | 0.430 μs | 19.13 μs | 20.63 μs | 21.08 μs |  1.04 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 23.52 μs | 0.226 μs | 0.200 μs | 23.23 μs | 23.50 μs | 23.83 μs |  1.19 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 19.71 μs | 0.385 μs | 0.514 μs | 18.91 μs | 19.88 μs | 20.58 μs |  1.00 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.30 μs | 0.416 μs | 0.480 μs | 20.33 μs | 21.52 μs | 21.80 μs |  1.08 |    0.04 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 22.92 μs | 0.435 μs | 0.407 μs | 21.80 μs | 23.06 μs | 23.34 μs |  1.16 |    0.04 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,213.538 μs | 12.6515 μs | 11.8342 μs | 1,179.120 μs | 1,217.725 μs | 1,226.749 μs | 1.000 |    3 | 5.8594 |      - |  95.51 KB |        1.00 |
| AutoMapperStartup |   237.566 μs |  0.8243 μs |  0.7307 μs |   236.271 μs |   237.336 μs |   238.771 μs | 0.196 |    2 | 5.8594 |      - | 104.28 KB |        1.09 |
| MapsterStartup    |     2.561 μs |  0.0081 μs |  0.0072 μs |     2.539 μs |     2.562 μs |     2.570 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.168 ms | 0.0122 ms | 0.0108 ms | 1.151 ms | 1.167 ms | 1.191 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  95.91 KB |        1.00 |
| AutoMapper | 3.426 ms | 0.0119 ms | 0.0111 ms | 3.409 ms | 3.423 ms | 3.448 ms |  2.93 |    0.03 |    3 | 15.6250 |  7.8125 |    310 KB |        3.23 |
| Mapster    | 2.587 ms | 0.0165 ms | 0.0154 ms | 2.570 ms | 2.583 ms | 2.625 ms |  2.22 |    0.02 |    2 | 39.0625 | 15.6250 | 757.49 KB |        7.90 |

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

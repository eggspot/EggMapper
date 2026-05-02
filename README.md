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
> ⏱ **Last updated:** 2026-05-02 08:53 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 17.93 ns | **27.74 ns (1.6×)** | 28.80 ns (1.6×) | 68.57 ns (3.8×) | 17.25 ns (1.0×) |
| **Flattening** | 19.12 ns | **30.64 ns (1.6×)** | 39.42 ns (2.1×) | 72.90 ns (3.8×) | 26.75 ns (1.4×) |
| **Deep (2 nested)** | 58.81 ns | **71.17 ns (1.2×)** | 80.10 ns (1.4×) | 113.17 ns (1.9×) | 58.97 ns (1.0×) |
| **Complex (nest+coll)** | 77.36 ns | **98.77 ns (1.3×)** | 93.74 ns (1.2×) | 148.11 ns (1.9×) | 77.82 ns (1.0×) |
| **Collection (100)** | 1.976 μs | **1.989 μs (1.0×)** | 1.918 μs (1.0×) | 2.485 μs (1.3×) | 2.134 μs (1.1×) |
| **Deep Coll (100)** | 6.076 μs | **6.187 μs (1.0×)** | 6.314 μs (1.0×) | 7.340 μs (1.2×) | 5.953 μs (1.0×) |
| **Large Coll (1000)** | 17.85 μs | **19.30 μs (1.1×)** | 17.63 μs (1.0×) | 24.56 μs (1.4×) | 21.75 μs (1.2×) |
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
| Manual               |  17.93 ns | 0.338 ns | 0.316 ns |  17.38 ns |  17.99 ns |  18.45 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapper            |  27.74 ns | 0.338 ns | 0.316 ns |  26.98 ns |  27.80 ns |  28.14 ns |  1.55 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| AutoMapper           |  68.57 ns | 0.332 ns | 0.311 ns |  67.97 ns |  68.65 ns |  69.00 ns |  3.83 |    0.07 |    3 | 0.0031 |      80 B |        1.00 |
| Mapster              |  28.80 ns | 0.344 ns | 0.322 ns |  28.24 ns |  28.74 ns |  29.43 ns |  1.61 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| MapperlyMap          |  17.25 ns | 0.375 ns | 0.350 ns |  16.82 ns |  17.15 ns |  18.02 ns |  0.96 |    0.03 |    1 | 0.0032 |      80 B |        1.00 |
| AgileMapper          | 346.31 ns | 0.501 ns | 0.469 ns | 345.55 ns | 346.29 ns | 347.17 ns | 19.32 |    0.33 |    4 | 0.0134 |     344 B |        4.30 |
| EggMapperGenerator   |  17.85 ns | 0.418 ns | 0.447 ns |  17.11 ns |  17.87 ns |  18.59 ns |  1.00 |    0.03 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapperClassMapper |  17.39 ns | 0.288 ns | 0.269 ns |  16.96 ns |  17.39 ns |  17.84 ns |  0.97 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.12 ns | 0.232 ns | 0.217 ns |  18.85 ns |  19.15 ns |  19.54 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  30.64 ns | 0.208 ns | 0.185 ns |  30.42 ns |  30.57 ns |  31.02 ns |  1.60 |    0.02 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  72.90 ns | 0.280 ns | 0.248 ns |  72.41 ns |  72.95 ns |  73.28 ns |  3.81 |    0.04 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  39.42 ns | 0.263 ns | 0.246 ns |  39.07 ns |  39.55 ns |  39.81 ns |  2.06 |    0.03 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  26.75 ns | 0.421 ns | 0.393 ns |  26.18 ns |  26.78 ns |  27.44 ns |  1.40 |    0.03 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 356.89 ns | 0.370 ns | 0.309 ns | 356.60 ns | 356.79 ns | 357.63 ns | 18.67 |    0.20 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  58.81 ns | 0.905 ns | 0.847 ns |  57.63 ns |  58.67 ns |  60.34 ns |  1.00 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  71.17 ns | 0.994 ns | 0.929 ns |  69.55 ns |  71.20 ns |  72.85 ns |  1.21 |    0.02 |    2 | 0.0107 |     272 B |        1.00 |
| AutoMapper  | 113.17 ns | 1.248 ns | 1.167 ns | 111.30 ns | 113.54 ns | 114.88 ns |  1.92 |    0.03 |    4 | 0.0107 |     272 B |        1.00 |
| Mapster     |  80.10 ns | 1.106 ns | 0.980 ns |  77.59 ns |  80.36 ns |  81.49 ns |  1.36 |    0.02 |    3 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  58.97 ns | 0.843 ns | 0.789 ns |  57.16 ns |  59.26 ns |  60.08 ns |  1.00 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| AgileMapper | 393.17 ns | 0.618 ns | 0.578 ns | 392.27 ns | 393.13 ns | 394.08 ns |  6.69 |    0.09 |    5 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  77.36 ns | 1.370 ns | 1.282 ns |  75.16 ns |  77.51 ns |  79.17 ns |  1.00 |    0.02 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   |  98.77 ns | 1.105 ns | 0.979 ns |  96.96 ns |  98.78 ns | 100.54 ns |  1.28 |    0.02 |    3 | 0.0126 |     320 B |        1.00 |
| AutoMapper  | 148.11 ns | 0.523 ns | 0.489 ns | 147.22 ns | 148.19 ns | 148.91 ns |  1.92 |    0.03 |    4 | 0.0129 |     328 B |        1.02 |
| Mapster     |  93.74 ns | 0.764 ns | 0.715 ns |  92.84 ns |  93.59 ns |  94.97 ns |  1.21 |    0.02 |    2 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  77.82 ns | 1.283 ns | 1.200 ns |  76.36 ns |  77.75 ns |  80.80 ns |  1.01 |    0.02 |    1 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 433.17 ns | 1.052 ns | 0.984 ns | 431.42 ns | 433.39 ns | 434.76 ns |  5.60 |    0.09 |    5 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.976 μs | 0.0382 μs | 0.0375 μs | 1.901 μs | 1.977 μs | 2.029 μs |  1.00 |    0.03 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| EggMapper   | 1.989 μs | 0.0329 μs | 0.0308 μs | 1.929 μs | 1.992 μs | 2.033 μs |  1.01 |    0.02 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AutoMapper  | 2.485 μs | 0.0356 μs | 0.0315 μs | 2.444 μs | 2.483 μs | 2.545 μs |  1.26 |    0.03 |    3 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.918 μs | 0.0346 μs | 0.0324 μs | 1.845 μs | 1.922 μs | 1.970 μs |  0.97 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| MapperlyMap | 2.134 μs | 0.0426 μs | 0.0437 μs | 2.064 μs | 2.130 μs | 2.204 μs |  1.08 |    0.03 |    2 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AgileMapper | 2.607 μs | 0.0326 μs | 0.0305 μs | 2.550 μs | 2.610 μs | 2.669 μs |  1.32 |    0.03 |    4 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.076 μs | 0.1199 μs | 0.1122 μs | 5.871 μs | 6.094 μs | 6.251 μs |  1.00 |    0.03 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 6.187 μs | 0.1198 μs | 0.1639 μs | 5.947 μs | 6.197 μs | 6.493 μs |  1.02 |    0.03 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| AutoMapper  | 7.340 μs | 0.1183 μs | 0.1106 μs | 7.197 μs | 7.319 μs | 7.557 μs |  1.21 |    0.03 |    3 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 6.314 μs | 0.1163 μs | 0.1031 μs | 6.081 μs | 6.329 μs | 6.481 μs |  1.04 |    0.02 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 5.953 μs | 0.1146 μs | 0.1177 μs | 5.801 μs | 5.955 μs | 6.113 μs |  0.98 |    0.03 |    2 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 5.518 μs | 0.0611 μs | 0.0572 μs | 5.414 μs | 5.528 μs | 5.594 μs |  0.91 |    0.02 |    1 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.85 μs | 0.203 μs | 0.180 μs | 17.61 μs | 17.85 μs | 18.17 μs |  1.00 |    0.01 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 19.30 μs | 0.201 μs | 0.178 μs | 19.08 μs | 19.30 μs | 19.58 μs |  1.08 |    0.01 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 24.56 μs | 0.226 μs | 0.212 μs | 24.10 μs | 24.56 μs | 24.95 μs |  1.38 |    0.02 |    4 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 17.63 μs | 0.321 μs | 0.300 μs | 17.11 μs | 17.55 μs | 18.24 μs |  0.99 |    0.02 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 21.75 μs | 0.310 μs | 0.290 μs | 21.35 μs | 21.67 μs | 22.28 μs |  1.22 |    0.02 |    3 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 22.22 μs | 0.214 μs | 0.200 μs | 21.79 μs | 22.20 μs | 22.51 μs |  1.25 |    0.02 |    3 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,162.040 μs | 7.2902 μs | 6.4626 μs | 1,150.654 μs | 1,162.926 μs | 1,172.448 μs | 1.000 |    3 | 3.9063 |      - |  95.67 KB |        1.00 |
| AutoMapperStartup |   259.904 μs | 0.5975 μs | 0.4989 μs |   259.020 μs |   259.958 μs |   260.553 μs | 0.224 |    2 | 3.9063 |      - | 103.76 KB |        1.08 |
| MapsterStartup    |     2.906 μs | 0.0389 μs | 0.0364 μs |     2.848 μs |     2.897 μs |     2.973 μs | 0.003 |    1 | 0.4692 | 0.0114 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.145 ms | 0.0099 ms | 0.0092 ms | 1.133 ms | 1.145 ms | 1.162 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  95.94 KB |        1.00 |
| AutoMapper | 3.310 ms | 0.0221 ms | 0.0207 ms | 3.242 ms | 3.312 ms | 3.328 ms |  2.89 |    0.03 |    3 |  7.8125 |       - | 309.91 KB |        3.23 |
| Mapster    | 2.492 ms | 0.0460 ms | 0.0430 ms | 2.456 ms | 2.469 ms | 2.575 ms |  2.18 |    0.04 |    2 | 23.4375 | 15.6250 | 758.49 KB |        7.91 |

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

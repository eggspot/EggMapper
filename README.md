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
> ⏱ **Last updated:** 2026-05-02 09:01 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 13.13 ns | **21.94 ns (1.7×)** | 23.27 ns (1.8×) | 60.25 ns (4.6×) | 13.47 ns (1.0×) |
| **Flattening** | 16.20 ns | **24.71 ns (1.5×)** | 27.95 ns (1.7×) | 67.44 ns (4.2×) | 20.21 ns (1.2×) |
| **Deep (2 nested)** | 46.04 ns | **55.57 ns (1.2×)** | 57.02 ns (1.2×) | 84.27 ns (1.8×) | 42.40 ns (0.9×) |
| **Complex (nest+coll)** | 63.91 ns | **88.69 ns (1.4×)** | 80.23 ns (1.3×) | 111.68 ns (1.8×) | 70.13 ns (1.1×) |
| **Collection (100)** | 1.579 μs | **1.651 μs (1.1×)** | 1.636 μs (1.0×) | 2.034 μs (1.3×) | 1.674 μs (1.1×) |
| **Deep Coll (100)** | 4.804 μs | **5.528 μs (1.1×)** | 5.344 μs (1.1×) | 5.863 μs (1.2×) | 5.313 μs (1.1×) |
| **Large Coll (1000)** | 17.51 μs | **18.86 μs (1.1×)** | 17.94 μs (1.0×) | 20.36 μs (1.2×) | 18.35 μs (1.1×) |
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
| Manual               |  13.13 ns | 0.159 ns | 0.141 ns |  12.93 ns |  13.10 ns |  13.43 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  21.94 ns | 0.301 ns | 0.267 ns |  21.38 ns |  21.95 ns |  22.23 ns |  1.67 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  60.25 ns | 0.293 ns | 0.259 ns |  59.65 ns |  60.35 ns |  60.50 ns |  4.59 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  23.27 ns | 0.393 ns | 0.367 ns |  22.59 ns |  23.42 ns |  23.88 ns |  1.77 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  13.47 ns | 0.321 ns | 0.428 ns |  12.55 ns |  13.64 ns |  13.91 ns |  1.03 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 281.61 ns | 1.841 ns | 1.632 ns | 279.39 ns | 281.34 ns | 284.14 ns | 21.46 |    0.25 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.76 ns | 0.219 ns | 0.205 ns |  12.39 ns |  12.78 ns |  13.05 ns |  0.97 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.74 ns | 0.112 ns | 0.105 ns |  12.59 ns |  12.75 ns |  12.95 ns |  0.97 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  16.20 ns | 0.257 ns | 0.240 ns |  15.76 ns |  16.26 ns |  16.50 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  24.71 ns | 0.220 ns | 0.205 ns |  24.35 ns |  24.72 ns |  25.08 ns |  1.53 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  67.44 ns | 0.110 ns | 0.103 ns |  67.21 ns |  67.44 ns |  67.64 ns |  4.16 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  27.95 ns | 0.420 ns | 0.393 ns |  27.52 ns |  27.77 ns |  28.83 ns |  1.73 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  20.21 ns | 0.124 ns | 0.110 ns |  20.03 ns |  20.21 ns |  20.38 ns |  1.25 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 269.82 ns | 2.374 ns | 2.221 ns | 266.27 ns | 269.38 ns | 273.66 ns | 16.66 |    0.28 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  46.04 ns | 0.192 ns | 0.170 ns |  45.78 ns |  46.00 ns |  46.37 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  55.57 ns | 0.297 ns | 0.248 ns |  54.94 ns |  55.67 ns |  55.85 ns |  1.21 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  |  84.27 ns | 0.253 ns | 0.224 ns |  83.78 ns |  84.25 ns |  84.59 ns |  1.83 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  57.02 ns | 0.389 ns | 0.364 ns |  56.46 ns |  57.00 ns |  57.62 ns |  1.24 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  42.40 ns | 0.502 ns | 0.469 ns |  41.82 ns |  42.33 ns |  43.21 ns |  0.92 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 290.79 ns | 1.406 ns | 1.174 ns | 288.66 ns | 290.93 ns | 293.12 ns |  6.32 |    0.03 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  63.91 ns | 0.635 ns | 0.594 ns |  62.36 ns |  63.88 ns |  64.93 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  88.69 ns | 1.622 ns | 1.518 ns |  86.44 ns |  88.71 ns |  91.82 ns |  1.39 |    0.03 |    4 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 111.68 ns | 1.412 ns | 1.251 ns | 110.16 ns | 111.32 ns | 114.18 ns |  1.75 |    0.02 |    5 | 0.0196 |     328 B |        1.02 |
| Mapster     |  80.23 ns | 0.560 ns | 0.524 ns |  79.28 ns |  80.43 ns |  81.04 ns |  1.26 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.13 ns | 0.907 ns | 0.848 ns |  68.45 ns |  70.03 ns |  71.59 ns |  1.10 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 339.01 ns | 3.254 ns | 3.044 ns | 333.53 ns | 339.65 ns | 344.31 ns |  5.31 |    0.07 |    6 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.579 μs | 0.0294 μs | 0.0261 μs | 1.539 μs | 1.584 μs | 1.640 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.651 μs | 0.0323 μs | 0.0302 μs | 1.587 μs | 1.666 μs | 1.686 μs |  1.05 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.034 μs | 0.0375 μs | 0.0350 μs | 1.929 μs | 2.040 μs | 2.071 μs |  1.29 |    0.03 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.636 μs | 0.0311 μs | 0.0319 μs | 1.548 μs | 1.643 μs | 1.682 μs |  1.04 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.674 μs | 0.0330 μs | 0.0441 μs | 1.585 μs | 1.670 μs | 1.759 μs |  1.06 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.212 μs | 0.0438 μs | 0.0487 μs | 2.121 μs | 2.221 μs | 2.319 μs |  1.40 |    0.04 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.804 μs | 0.0871 μs | 0.1003 μs | 4.628 μs | 4.797 μs | 4.975 μs |  1.00 |    0.03 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.528 μs | 0.0891 μs | 0.0834 μs | 5.376 μs | 5.530 μs | 5.670 μs |  1.15 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 5.863 μs | 0.1159 μs | 0.1735 μs | 5.546 μs | 5.912 μs | 6.125 μs |  1.22 |    0.04 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.344 μs | 0.0886 μs | 0.0786 μs | 5.168 μs | 5.362 μs | 5.456 μs |  1.11 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.313 μs | 0.0921 μs | 0.0861 μs | 5.194 μs | 5.303 μs | 5.470 μs |  1.11 |    0.03 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.906 μs | 0.0792 μs | 0.0741 μs | 4.771 μs | 4.928 μs | 4.998 μs |  1.02 |    0.03 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.51 μs | 0.348 μs | 0.696 μs | 17.52 μs | 15.99 μs | 18.56 μs |  1.00 |    0.06 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.86 μs | 0.375 μs | 0.561 μs | 18.89 μs | 16.94 μs | 19.81 μs |  1.08 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.36 μs | 0.440 μs | 1.298 μs | 20.02 μs | 18.26 μs | 23.12 μs |  1.16 |    0.09 |    1 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.94 μs | 0.356 μs | 0.575 μs | 18.20 μs | 16.88 μs | 18.81 μs |  1.03 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.35 μs | 0.358 μs | 0.589 μs | 18.33 μs | 17.00 μs | 19.44 μs |  1.05 |    0.05 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 18.95 μs | 0.318 μs | 0.327 μs | 19.01 μs | 18.24 μs | 19.34 μs |  1.08 |    0.05 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 923.353 μs | 8.3407 μs | 7.8019 μs | 912.815 μs | 921.637 μs | 943.063 μs | 1.000 |    3 | 3.9063 | 1.9531 |  94.97 KB |        1.00 |
| AutoMapperStartup | 188.567 μs | 1.5072 μs | 1.3361 μs | 186.642 μs | 188.561 μs | 191.042 μs | 0.204 |    2 | 5.8594 |      - | 104.21 KB |        1.10 |
| MapsterStartup    |   2.238 μs | 0.0383 μs | 0.0358 μs |   2.191 μs |   2.238 μs |   2.314 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error    | StdDev  | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|---------:|--------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   924.9 μs |  9.01 μs | 8.43 μs |   914.8 μs |   924.5 μs |   939.5 μs |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |   95.9 KB |        1.00 |
| AutoMapper | 2,653.6 μs |  9.34 μs | 8.28 μs | 2,643.5 μs | 2,653.0 μs | 2,669.8 μs |  2.87 |    0.03 |    3 | 15.6250 |  7.8125 | 310.28 KB |        3.24 |
| Mapster    | 1,992.9 μs | 10.63 μs | 9.42 μs | 1,974.1 μs | 1,991.4 μs | 2,006.8 μs |  2.15 |    0.02 |    2 | 42.9688 | 19.5313 | 764.06 KB |        7.97 |

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

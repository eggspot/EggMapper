# 🥚 EggMapper

> **The fastest .NET runtime object-to-object mapper** — forked from AutoMapper's last open-source release, rebuilt for maximum performance. Drop-in replacement with the same API, 1.5–5× faster.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

📖 **[Full documentation →](https://github.com/eggspot/EggMapper/wiki)**

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
> ⏱ **Last updated:** 2026-03-25 06:13 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.19 ns | **27.15 ns (1.8×)** | 28.63 ns (1.9×) | 82.34 ns (5.4×) | 16.22 ns (1.1×) |
| **Flattening** | 20.18 ns | **30.59 ns (1.5×)** | 49.79 ns (2.5×) | 89.51 ns (4.4×) | 24.68 ns (1.2×) |
| **Deep (2 nested)** | 54.32 ns | **67.87 ns (1.2×)** | 71.60 ns (1.3×) | 127.98 ns (2.4×) | 50.48 ns (0.9×) |
| **Complex (nest+coll)** | 69.90 ns | **90.48 ns (1.3×)** | 95.30 ns (1.4×) | 156.35 ns (2.2×) | 75.42 ns (1.1×) |
| **Collection (100)** | 1.812 μs | **1.741 μs (1.0×)** | 1.770 μs (1.0×) | 2.347 μs (1.3×) | 1.928 μs (1.1×) |
| **Deep Coll (100)** | 5.764 μs | **5.971 μs (1.0×)** | 6.116 μs (1.1×) | 6.843 μs (1.2×) | 6.239 μs (1.1×) |
| **Large Coll (1000)** | 17.96 μs | **17.88 μs (1.0×)** | 17.80 μs (1.0×) | 21.84 μs (1.2×) | 18.70 μs (1.0×) |
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
| Manual               |  15.19 ns | 0.194 ns | 0.182 ns |  14.94 ns |  15.20 ns |  15.49 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.15 ns | 0.451 ns | 0.422 ns |  26.13 ns |  27.31 ns |  27.54 ns |  1.79 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  82.34 ns | 0.700 ns | 0.654 ns |  81.02 ns |  82.54 ns |  83.33 ns |  5.42 |    0.08 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.63 ns | 0.367 ns | 0.343 ns |  28.27 ns |  28.51 ns |  29.45 ns |  1.89 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.22 ns | 0.273 ns | 0.256 ns |  15.68 ns |  16.22 ns |  16.62 ns |  1.07 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 500.54 ns | 2.219 ns | 2.076 ns | 496.92 ns | 500.33 ns | 503.88 ns | 32.97 |    0.40 |    6 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  16.66 ns | 0.395 ns | 0.638 ns |  16.03 ns |  16.37 ns |  17.86 ns |  1.10 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.80 ns | 0.159 ns | 0.149 ns |  16.52 ns |  16.84 ns |  16.96 ns |  1.11 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.18 ns | 0.137 ns | 0.121 ns |  19.86 ns |  20.21 ns |  20.30 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.59 ns | 0.357 ns | 0.334 ns |  29.93 ns |  30.62 ns |  31.08 ns |  1.52 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  89.51 ns | 0.239 ns | 0.223 ns |  88.98 ns |  89.60 ns |  89.77 ns |  4.44 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  49.79 ns | 0.415 ns | 0.388 ns |  49.10 ns |  49.80 ns |  50.28 ns |  2.47 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.68 ns | 0.497 ns | 0.511 ns |  23.91 ns |  24.70 ns |  25.73 ns |  1.22 |    0.03 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 548.65 ns | 0.946 ns | 0.885 ns | 547.48 ns | 548.40 ns | 550.46 ns | 27.19 |    0.16 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  54.32 ns | 0.929 ns | 0.869 ns |  52.59 ns |  54.48 ns |  55.65 ns |  1.00 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  67.87 ns | 1.325 ns | 1.360 ns |  64.55 ns |  68.47 ns |  69.17 ns |  1.25 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 127.98 ns | 1.397 ns | 1.306 ns | 125.30 ns | 127.86 ns | 130.42 ns |  2.36 |    0.04 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  71.60 ns | 0.919 ns | 0.860 ns |  69.89 ns |  71.79 ns |  72.68 ns |  1.32 |    0.03 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  50.48 ns | 0.596 ns | 0.497 ns |  49.46 ns |  50.45 ns |  51.37 ns |  0.93 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 508.92 ns | 4.147 ns | 3.676 ns | 502.69 ns | 508.41 ns | 514.91 ns |  9.37 |    0.16 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  69.90 ns | 0.663 ns | 0.518 ns |  69.05 ns |  69.82 ns |  70.62 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  90.48 ns | 1.807 ns | 2.591 ns |  87.00 ns |  89.75 ns |  95.62 ns |  1.29 |    0.04 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 156.35 ns | 1.599 ns | 1.418 ns | 153.02 ns | 156.19 ns | 158.97 ns |  2.24 |    0.03 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  95.30 ns | 1.166 ns | 1.090 ns |  93.78 ns |  95.19 ns |  97.20 ns |  1.36 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  75.42 ns | 0.712 ns | 0.666 ns |  74.03 ns |  75.28 ns |  76.35 ns |  1.08 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 566.61 ns | 3.247 ns | 3.038 ns | 561.69 ns | 567.88 ns | 570.58 ns |  8.11 |    0.07 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.812 μs | 0.0261 μs | 0.0244 μs | 1.767 μs | 1.817 μs | 1.847 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.741 μs | 0.0338 μs | 0.0347 μs | 1.685 μs | 1.738 μs | 1.804 μs |  0.96 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.347 μs | 0.0209 μs | 0.0186 μs | 2.315 μs | 2.349 μs | 2.386 μs |  1.30 |    0.02 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.770 μs | 0.0287 μs | 0.0268 μs | 1.728 μs | 1.766 μs | 1.814 μs |  0.98 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.928 μs | 0.0385 μs | 0.0458 μs | 1.867 μs | 1.933 μs | 2.019 μs |  1.06 |    0.03 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.653 μs | 0.0398 μs | 0.0373 μs | 2.587 μs | 2.649 μs | 2.723 μs |  1.46 |    0.03 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.764 μs | 0.1148 μs | 0.1276 μs | 5.458 μs | 5.779 μs | 5.959 μs |  1.00 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.971 μs | 0.1095 μs | 0.1025 μs | 5.803 μs | 5.975 μs | 6.157 μs |  1.04 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.843 μs | 0.1338 μs | 0.1786 μs | 6.421 μs | 6.904 μs | 7.080 μs |  1.19 |    0.04 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.116 μs | 0.1187 μs | 0.1413 μs | 5.868 μs | 6.102 μs | 6.356 μs |  1.06 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.239 μs | 0.0950 μs | 0.0889 μs | 6.093 μs | 6.247 μs | 6.354 μs |  1.08 |    0.03 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.310 μs | 0.1018 μs | 0.0952 μs | 5.106 μs | 5.356 μs | 5.441 μs |  0.92 |    0.03 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.96 μs | 0.359 μs | 0.413 μs | 17.40 μs | 17.99 μs | 18.80 μs |  1.00 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.88 μs | 0.350 μs | 0.416 μs | 17.27 μs | 17.74 μs | 18.63 μs |  1.00 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.84 μs | 0.370 μs | 0.564 μs | 20.91 μs | 21.67 μs | 23.51 μs |  1.22 |    0.04 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.80 μs | 0.277 μs | 0.245 μs | 17.31 μs | 17.86 μs | 18.10 μs |  0.99 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.70 μs | 0.167 μs | 0.140 μs | 18.41 μs | 18.68 μs | 18.95 μs |  1.04 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.52 μs | 0.269 μs | 0.251 μs | 20.02 μs | 20.52 μs | 20.90 μs |  1.14 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,115.123 μs | 5.6053 μs | 4.9690 μs | 1,107.040 μs | 1,115.902 μs | 1,123.847 μs | 1.000 |    3 | 3.9063 | 1.9531 |  94.65 KB |        1.00 |
| AutoMapperStartup |   280.078 μs | 1.0475 μs | 0.8178 μs |   278.891 μs |   279.918 μs |   281.605 μs | 0.251 |    2 | 5.8594 |      - | 104.27 KB |        1.10 |
| MapsterStartup    |     2.589 μs | 0.0495 μs | 0.0589 μs |     2.503 μs |     2.589 μs |     2.708 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.161 ms | 0.0149 ms | 0.0139 ms | 1.130 ms | 1.161 ms | 1.181 ms |  1.00 |    0.02 |    1 |  5.8594 |       - |  95.48 KB |        1.00 |
| AutoMapper | 3.223 ms | 0.0184 ms | 0.0172 ms | 3.201 ms | 3.221 ms | 3.253 ms |  2.78 |    0.04 |    3 | 15.6250 |  7.8125 | 310.07 KB |        3.25 |
| Mapster    | 2.493 ms | 0.0113 ms | 0.0105 ms | 2.478 ms | 2.493 ms | 2.514 ms |  2.15 |    0.03 |    2 | 39.0625 | 15.6250 | 759.32 KB |        7.95 |

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

| | **Runtime** (`EggMapper`) | **Tier 2** (`EggMapper.Generator`) | **Tier 3** (`EggMapper.ClassMapper`) |
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

See [Migration Guide](docs/Migration-Guide.md) to move from runtime to compile-time APIs.

---

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](https://github.com/eggspot/EggMapper/wiki/Getting-Started) | Installation and your first runtime mapping |
| [Tier 2 Getting Started](docs/Tier2-Getting-Started.md) | Compile-time extension methods with `[MapTo]` |
| [Tier 3 Getting Started](docs/Tier3-Getting-Started.md) | Compile-time partial mapper classes with `[EggMapper]` |
| [Migration Guide](docs/Migration-Guide.md) | Moving from runtime to compile-time APIs |
| [Diagnostic Reference](docs/diagnostics/) | All EGG diagnostic codes explained |
| [Configuration](https://github.com/eggspot/EggMapper/wiki/Configuration) | `MapperConfiguration` options |
| [Profiles](https://github.com/eggspot/EggMapper/wiki/Profiles) | Organising maps with `Profile` |
| [Dependency Injection](https://github.com/eggspot/EggMapper/wiki/Dependency-Injection) | ASP.NET Core / DI integration |
| [Advanced Features](https://github.com/eggspot/EggMapper/wiki/Advanced-Features) | `ForMember`, conditions, hooks, etc. |
| [Performance](https://github.com/eggspot/EggMapper/wiki/Performance) | Benchmark methodology & tips |
| [API Reference](https://github.com/eggspot/EggMapper/wiki/API-Reference) | Full public API surface |

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
- **Improve docs** — Edit files in the `docs/` folder (auto-synced to the wiki)
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

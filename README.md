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
> ⏱ **Last updated:** 2026-03-25 05:39 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.63 ns | **27.75 ns (1.7×)** | 29.58 ns (1.8×) | 86.85 ns (5.2×) | 16.35 ns (1.0×) |
| **Flattening** | 19.66 ns | **30.05 ns (1.5×)** | 38.47 ns (2.0×) | 88.24 ns (4.5×) | 25.19 ns (1.3×) |
| **Deep (2 nested)** | 55.18 ns | **64.16 ns (1.2×)** | 71.44 ns (1.3×) | 121.98 ns (2.2×) | 51.61 ns (0.9×) |
| **Complex (nest+coll)** | 71.53 ns | **91.99 ns (1.3×)** | 91.60 ns (1.3×) | 154.21 ns (2.2×) | 70.30 ns (1.0×) |
| **Collection (100)** | 1.819 μs | **1.855 μs (1.0×)** | 1.801 μs (1.0×) | 2.433 μs (1.3×) | 1.849 μs (1.0×) |
| **Deep Coll (100)** | 5.290 μs | **5.925 μs (1.1×)** | 5.844 μs (1.1×) | 6.394 μs (1.2×) | 5.327 μs (1.0×) |
| **Large Coll (1000)** | 18.16 μs | **17.52 μs (1.0×)** | 17.23 μs (0.9×) | 22.06 μs (1.2×) | 18.61 μs (1.0×) |
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
| Manual               |  16.63 ns | 0.288 ns | 0.255 ns |  16.37 ns |  16.59 ns |  17.21 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.75 ns | 0.315 ns | 0.295 ns |  27.28 ns |  27.72 ns |  28.22 ns |  1.67 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  86.85 ns | 0.299 ns | 0.250 ns |  86.26 ns |  86.90 ns |  87.24 ns |  5.22 |    0.08 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.58 ns | 0.286 ns | 0.253 ns |  29.00 ns |  29.65 ns |  29.85 ns |  1.78 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.35 ns | 0.200 ns | 0.187 ns |  15.95 ns |  16.34 ns |  16.62 ns |  0.98 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 483.86 ns | 1.568 ns | 1.310 ns | 482.62 ns | 483.20 ns | 487.24 ns | 29.10 |    0.43 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  16.63 ns | 0.151 ns | 0.141 ns |  16.32 ns |  16.65 ns |  16.82 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.95 ns | 0.194 ns | 0.182 ns |  16.49 ns |  17.01 ns |  17.18 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.66 ns | 0.210 ns | 0.176 ns |  19.44 ns |  19.61 ns |  20.09 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.05 ns | 0.490 ns | 0.458 ns |  29.31 ns |  30.22 ns |  30.61 ns |  1.53 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  88.24 ns | 0.609 ns | 0.570 ns |  87.39 ns |  88.48 ns |  88.98 ns |  4.49 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  38.47 ns | 0.154 ns | 0.144 ns |  38.17 ns |  38.49 ns |  38.65 ns |  1.96 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  25.19 ns | 0.344 ns | 0.305 ns |  24.41 ns |  25.24 ns |  25.64 ns |  1.28 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 531.63 ns | 1.563 ns | 1.462 ns | 529.31 ns | 531.19 ns | 534.56 ns | 27.05 |    0.24 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  55.18 ns | 1.159 ns | 1.138 ns |  53.54 ns |  55.08 ns |  56.98 ns |  1.00 |    0.03 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.16 ns | 0.346 ns | 0.270 ns |  63.51 ns |  64.19 ns |  64.59 ns |  1.16 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 121.98 ns | 1.852 ns | 1.732 ns | 119.24 ns | 121.97 ns | 124.62 ns |  2.21 |    0.05 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  71.44 ns | 1.410 ns | 1.732 ns |  68.59 ns |  71.42 ns |  73.93 ns |  1.30 |    0.04 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  51.61 ns | 1.075 ns | 1.826 ns |  49.17 ns |  50.88 ns |  55.80 ns |  0.94 |    0.04 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 516.50 ns | 2.473 ns | 2.314 ns | 513.33 ns | 516.89 ns | 520.32 ns |  9.36 |    0.19 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  71.53 ns | 0.811 ns | 0.758 ns |  70.44 ns |  71.56 ns |  73.08 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  91.99 ns | 0.609 ns | 0.570 ns |  91.33 ns |  91.83 ns |  93.24 ns |  1.29 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 154.21 ns | 1.054 ns | 0.985 ns | 151.71 ns | 154.30 ns | 155.73 ns |  2.16 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  91.60 ns | 1.170 ns | 1.037 ns |  89.99 ns |  91.74 ns |  93.39 ns |  1.28 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.30 ns | 0.631 ns | 0.527 ns |  69.47 ns |  70.29 ns |  71.49 ns |  0.98 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 553.99 ns | 6.088 ns | 5.695 ns | 546.25 ns | 553.23 ns | 565.16 ns |  7.75 |    0.11 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.819 μs | 0.0187 μs | 0.0175 μs | 1.779 μs | 1.820 μs | 1.848 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.855 μs | 0.0369 μs | 0.0379 μs | 1.804 μs | 1.849 μs | 1.928 μs |  1.02 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.433 μs | 0.0394 μs | 0.0369 μs | 2.382 μs | 2.424 μs | 2.508 μs |  1.34 |    0.02 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.801 μs | 0.0213 μs | 0.0200 μs | 1.767 μs | 1.800 μs | 1.825 μs |  0.99 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.849 μs | 0.0358 μs | 0.0352 μs | 1.807 μs | 1.845 μs | 1.909 μs |  1.02 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.552 μs | 0.0338 μs | 0.0283 μs | 2.492 μs | 2.552 μs | 2.604 μs |  1.40 |    0.02 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.290 μs | 0.1055 μs | 0.1172 μs | 5.077 μs | 5.323 μs | 5.486 μs |  1.00 |    0.03 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.925 μs | 0.1159 μs | 0.1466 μs | 5.611 μs | 5.918 μs | 6.168 μs |  1.12 |    0.04 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.394 μs | 0.0567 μs | 0.0474 μs | 6.296 μs | 6.395 μs | 6.501 μs |  1.21 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.844 μs | 0.1098 μs | 0.1307 μs | 5.634 μs | 5.809 μs | 6.079 μs |  1.11 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.327 μs | 0.0353 μs | 0.0313 μs | 5.270 μs | 5.325 μs | 5.389 μs |  1.01 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.196 μs | 0.0209 μs | 0.0164 μs | 5.159 μs | 5.200 μs | 5.217 μs |  0.98 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.16 μs | 0.337 μs | 0.315 μs | 17.61 μs | 18.17 μs | 18.68 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.52 μs | 0.194 μs | 0.182 μs | 17.27 μs | 17.50 μs | 17.91 μs |  0.97 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 22.06 μs | 0.435 μs | 0.501 μs | 21.30 μs | 22.10 μs | 22.78 μs |  1.21 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.23 μs | 0.229 μs | 0.215 μs | 16.77 μs | 17.27 μs | 17.51 μs |  0.95 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.61 μs | 0.371 μs | 0.620 μs | 17.76 μs | 18.58 μs | 20.13 μs |  1.03 |    0.04 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.58 μs | 0.165 μs | 0.154 μs | 19.33 μs | 19.53 μs | 19.89 μs |  1.08 |    0.02 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,054.582 μs | 5.8534 μs | 5.1889 μs | 1,046.735 μs | 1,053.748 μs | 1,061.090 μs | 1.000 |    3 | 3.9063 | 1.9531 |  93.41 KB |        1.00 |
| AutoMapperStartup |   279.449 μs | 0.7009 μs | 0.5472 μs |   278.005 μs |   279.599 μs |   280.021 μs | 0.265 |    2 | 5.8594 |      - | 103.78 KB |        1.11 |
| MapsterStartup    |     2.456 μs | 0.0304 μs | 0.0284 μs |     2.408 μs |     2.452 μs |     2.517 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.058 ms | 0.0125 ms | 0.0117 ms | 1.045 ms | 1.052 ms | 1.086 ms |  1.00 |    0.02 |    1 |  3.9063 |  1.9531 |  94.25 KB |        1.00 |
| AutoMapper | 3.192 ms | 0.0086 ms | 0.0067 ms | 3.179 ms | 3.193 ms | 3.200 ms |  3.02 |    0.03 |    3 | 15.6250 |  7.8125 | 309.75 KB |        3.29 |
| Mapster    | 2.499 ms | 0.0147 ms | 0.0123 ms | 2.486 ms | 2.495 ms | 2.532 ms |  2.36 |    0.03 |    2 | 39.0625 | 15.6250 | 764.22 KB |        8.11 |

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

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
> ⏱ **Last updated:** 2026-03-27 14:14 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.18 ns | **26.37 ns (1.6×)** | 29.60 ns (1.8×) | 83.46 ns (5.2×) | 16.35 ns (1.0×) |
| **Flattening** | 20.13 ns | **30.75 ns (1.5×)** | 49.36 ns (2.5×) | 89.57 ns (4.5×) | 25.17 ns (1.2×) |
| **Deep (2 nested)** | 57.31 ns | **68.33 ns (1.2×)** | 71.55 ns (1.2×) | 130.89 ns (2.3×) | 52.95 ns (0.9×) |
| **Complex (nest+coll)** | 75.37 ns | **96.50 ns (1.3×)** | 94.28 ns (1.2×) | 157.61 ns (2.1×) | 75.47 ns (1.0×) |
| **Collection (100)** | 1.916 μs | **1.864 μs (1.0×)** | 1.850 μs (1.0×) | 2.510 μs (1.3×) | 1.995 μs (1.0×) |
| **Deep Coll (100)** | 6.277 μs | **6.111 μs (1.0×)** | 6.482 μs (1.0×) | 6.917 μs (1.1×) | 5.772 μs (0.9×) |
| **Large Coll (1000)** | 19.30 μs | **18.85 μs (1.0×)** | 18.52 μs (1.0×) | 22.90 μs (1.2×) | 22.06 μs (1.1×) |
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
| Manual               |  16.18 ns | 0.167 ns | 0.157 ns |  15.94 ns |  16.15 ns |  16.48 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.37 ns | 0.190 ns | 0.169 ns |  26.05 ns |  26.38 ns |  26.72 ns |  1.63 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  83.46 ns | 0.256 ns | 0.239 ns |  83.01 ns |  83.47 ns |  83.90 ns |  5.16 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.60 ns | 0.187 ns | 0.166 ns |  29.28 ns |  29.56 ns |  29.84 ns |  1.83 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.35 ns | 0.196 ns | 0.184 ns |  16.10 ns |  16.29 ns |  16.62 ns |  1.01 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 514.51 ns | 1.664 ns | 1.557 ns | 511.60 ns | 514.79 ns | 516.59 ns | 31.80 |    0.31 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.94 ns | 0.178 ns | 0.167 ns |  15.53 ns |  15.98 ns |  16.15 ns |  0.99 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.74 ns | 0.108 ns | 0.090 ns |  16.63 ns |  16.74 ns |  16.91 ns |  1.03 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.13 ns | 0.174 ns | 0.154 ns |  19.87 ns |  20.13 ns |  20.42 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.75 ns | 0.244 ns | 0.204 ns |  30.13 ns |  30.78 ns |  30.95 ns |  1.53 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  89.57 ns | 0.206 ns | 0.172 ns |  89.34 ns |  89.60 ns |  89.86 ns |  4.45 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  49.36 ns | 0.231 ns | 0.205 ns |  48.99 ns |  49.34 ns |  49.67 ns |  2.45 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  25.17 ns | 0.132 ns | 0.123 ns |  24.97 ns |  25.13 ns |  25.38 ns |  1.25 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 529.57 ns | 1.187 ns | 1.110 ns | 527.31 ns | 529.50 ns | 531.21 ns | 26.31 |    0.20 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  57.31 ns | 0.494 ns | 0.412 ns |  56.76 ns |  57.27 ns |  58.15 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  68.33 ns | 0.512 ns | 0.454 ns |  67.57 ns |  68.38 ns |  69.04 ns |  1.19 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 130.89 ns | 0.631 ns | 0.591 ns | 129.93 ns | 130.87 ns | 131.96 ns |  2.28 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  71.55 ns | 0.646 ns | 0.604 ns |  70.27 ns |  71.60 ns |  72.60 ns |  1.25 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  52.95 ns | 0.315 ns | 0.279 ns |  52.35 ns |  52.97 ns |  53.30 ns |  0.92 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 514.21 ns | 1.865 ns | 1.744 ns | 512.00 ns | 513.78 ns | 517.66 ns |  8.97 |    0.07 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  75.37 ns | 0.466 ns | 0.436 ns |  74.75 ns |  75.41 ns |  76.16 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  96.50 ns | 0.543 ns | 0.481 ns |  95.84 ns |  96.42 ns |  97.25 ns |  1.28 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 157.61 ns | 0.971 ns | 0.908 ns | 156.13 ns | 157.62 ns | 159.36 ns |  2.09 |    0.02 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  94.28 ns | 0.975 ns | 0.912 ns |  92.22 ns |  94.66 ns |  95.59 ns |  1.25 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  75.47 ns | 0.740 ns | 0.692 ns |  74.61 ns |  75.26 ns |  76.41 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 582.33 ns | 1.241 ns | 1.100 ns | 580.78 ns | 582.43 ns | 584.49 ns |  7.73 |    0.05 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.916 μs | 0.0089 μs | 0.0074 μs | 1.901 μs | 1.916 μs | 1.927 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.864 μs | 0.0287 μs | 0.0269 μs | 1.812 μs | 1.868 μs | 1.905 μs |  0.97 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.510 μs | 0.0425 μs | 0.0398 μs | 2.459 μs | 2.521 μs | 2.584 μs |  1.31 |    0.02 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.850 μs | 0.0358 μs | 0.0335 μs | 1.799 μs | 1.841 μs | 1.914 μs |  0.97 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.995 μs | 0.0136 μs | 0.0127 μs | 1.967 μs | 1.994 μs | 2.019 μs |  1.04 |    0.01 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.685 μs | 0.0188 μs | 0.0157 μs | 2.661 μs | 2.687 μs | 2.715 μs |  1.40 |    0.01 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.277 μs | 0.0681 μs | 0.0637 μs | 6.168 μs | 6.275 μs | 6.380 μs |  1.00 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.111 μs | 0.0636 μs | 0.0595 μs | 5.996 μs | 6.122 μs | 6.191 μs |  0.97 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.917 μs | 0.0447 μs | 0.0396 μs | 6.822 μs | 6.912 μs | 6.992 μs |  1.10 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.482 μs | 0.0742 μs | 0.0694 μs | 6.392 μs | 6.482 μs | 6.629 μs |  1.03 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.772 μs | 0.0491 μs | 0.0459 μs | 5.675 μs | 5.783 μs | 5.854 μs |  0.92 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.505 μs | 0.0411 μs | 0.0365 μs | 5.468 μs | 5.491 μs | 5.597 μs |  0.88 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.30 μs | 0.208 μs | 0.184 μs | 19.06 μs | 19.29 μs | 19.69 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.85 μs | 0.162 μs | 0.135 μs | 18.55 μs | 18.88 μs | 19.08 μs |  0.98 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 22.90 μs | 0.225 μs | 0.211 μs | 22.51 μs | 22.89 μs | 23.22 μs |  1.19 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.52 μs | 0.155 μs | 0.145 μs | 18.28 μs | 18.49 μs | 18.71 μs |  0.96 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 22.06 μs | 0.172 μs | 0.161 μs | 21.80 μs | 22.02 μs | 22.38 μs |  1.14 |    0.01 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 21.57 μs | 0.266 μs | 0.236 μs | 21.17 μs | 21.58 μs | 21.97 μs |  1.12 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,199.826 μs | 6.7724 μs | 6.3350 μs | 1,190.649 μs | 1,200.022 μs | 1,210.590 μs | 1.000 |    3 | 5.8594 |      - |  95.69 KB |        1.00 |
| AutoMapperStartup |   282.899 μs | 1.2716 μs | 1.1272 μs |   280.515 μs |   282.702 μs |   285.338 μs | 0.236 |    2 | 5.8594 |      - | 104.04 KB |        1.09 |
| MapsterStartup    |     2.639 μs | 0.0329 μs | 0.0292 μs |     2.592 μs |     2.641 μs |     2.695 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.165 ms | 0.0100 ms | 0.0094 ms | 1.154 ms | 1.165 ms | 1.184 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  95.98 KB |        1.00 |
| AutoMapper | 3.297 ms | 0.0157 ms | 0.0139 ms | 3.278 ms | 3.295 ms | 3.323 ms |  2.83 |    0.02 |    3 | 15.6250 |  7.8125 | 310.17 KB |        3.23 |
| Mapster    | 2.552 ms | 0.0129 ms | 0.0114 ms | 2.529 ms | 2.555 ms | 2.569 ms |  2.19 |    0.02 |    2 | 39.0625 | 15.6250 | 764.06 KB |        7.96 |

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

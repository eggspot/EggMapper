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
> ⏱ **Last updated:** 2026-05-01 17:19 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.28 ns | **26.69 ns (1.6×)** | 28.77 ns (1.8×) | 81.78 ns (5.0×) | 16.24 ns (1.0×) |
| **Flattening** | 20.32 ns | **31.40 ns (1.6×)** | 34.92 ns (1.7×) | 81.19 ns (4.0×) | 25.77 ns (1.3×) |
| **Deep (2 nested)** | 57.85 ns | **73.59 ns (1.3×)** | 72.58 ns (1.2×) | 109.06 ns (1.9×) | 56.32 ns (1.0×) |
| **Complex (nest+coll)** | 82.74 ns | **107.48 ns (1.3×)** | 103.45 ns (1.2×) | 146.89 ns (1.8×) | 80.72 ns (1.0×) |
| **Collection (100)** | 2.088 μs | **2.171 μs (1.0×)** | 2.032 μs (1.0×) | 2.650 μs (1.3×) | 2.242 μs (1.1×) |
| **Deep Coll (100)** | 6.001 μs | **6.398 μs (1.1×)** | 6.508 μs (1.1×) | 7.153 μs (1.2×) | 5.946 μs (1.0×) |
| **Large Coll (1000)** | 21.92 μs | **21.75 μs (1.0×)** | 21.00 μs (1.0×) | 24.75 μs (1.1×) | 21.99 μs (1.0×) |
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
| Manual               |  16.28 ns | 0.216 ns | 0.191 ns |  15.94 ns |  16.27 ns |  16.63 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.69 ns | 0.168 ns | 0.141 ns |  26.38 ns |  26.69 ns |  26.91 ns |  1.64 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  81.78 ns | 0.141 ns | 0.125 ns |  81.48 ns |  81.79 ns |  81.98 ns |  5.02 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.77 ns | 0.317 ns | 0.296 ns |  28.38 ns |  28.71 ns |  29.28 ns |  1.77 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.24 ns | 0.160 ns | 0.142 ns |  15.93 ns |  16.29 ns |  16.40 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 333.59 ns | 1.334 ns | 1.248 ns | 331.09 ns | 333.45 ns | 335.67 ns | 20.49 |    0.24 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  16.42 ns | 0.205 ns | 0.192 ns |  16.00 ns |  16.46 ns |  16.70 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  17.27 ns | 0.215 ns | 0.201 ns |  16.89 ns |  17.29 ns |  17.60 ns |  1.06 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.32 ns | 0.155 ns | 0.137 ns |  20.15 ns |  20.27 ns |  20.59 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.40 ns | 0.366 ns | 0.342 ns |  31.00 ns |  31.26 ns |  32.16 ns |  1.55 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  81.19 ns | 0.231 ns | 0.205 ns |  80.86 ns |  81.19 ns |  81.55 ns |  4.00 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  34.92 ns | 0.101 ns | 0.084 ns |  34.81 ns |  34.88 ns |  35.10 ns |  1.72 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  25.77 ns | 0.361 ns | 0.338 ns |  25.43 ns |  25.67 ns |  26.19 ns |  1.27 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 333.89 ns | 1.756 ns | 1.557 ns | 330.43 ns | 333.76 ns | 336.87 ns | 16.43 |    0.13 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  57.85 ns | 0.127 ns | 0.113 ns |  57.73 ns |  57.83 ns |  58.06 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  73.59 ns | 0.491 ns | 0.459 ns |  72.83 ns |  73.65 ns |  74.32 ns |  1.27 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 109.06 ns | 0.164 ns | 0.145 ns | 108.84 ns | 109.04 ns | 109.32 ns |  1.89 |    0.00 |    3 | 0.0162 |     272 B |        1.00 |
| Mapster     |  72.58 ns | 0.189 ns | 0.177 ns |  72.41 ns |  72.54 ns |  72.90 ns |  1.25 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  56.32 ns | 0.674 ns | 0.630 ns |  55.47 ns |  56.28 ns |  57.56 ns |  0.97 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 412.46 ns | 0.983 ns | 0.871 ns | 410.56 ns | 412.52 ns | 414.07 ns |  7.13 |    0.02 |    4 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  82.74 ns | 0.525 ns | 0.491 ns |  81.76 ns |  82.98 ns |  83.23 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 107.48 ns | 0.952 ns | 0.890 ns | 105.74 ns | 107.61 ns | 108.67 ns |  1.30 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 146.89 ns | 1.469 ns | 1.375 ns | 144.72 ns | 146.71 ns | 149.89 ns |  1.78 |    0.02 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     | 103.45 ns | 0.702 ns | 0.622 ns | 102.19 ns | 103.41 ns | 104.51 ns |  1.25 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  80.72 ns | 0.719 ns | 0.673 ns |  79.42 ns |  80.91 ns |  81.64 ns |  0.98 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 397.18 ns | 1.295 ns | 1.082 ns | 395.51 ns | 397.19 ns | 399.32 ns |  4.80 |    0.03 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.088 μs | 0.0417 μs | 0.1113 μs | 1.932 μs | 2.072 μs | 2.372 μs |  1.00 |    0.07 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.171 μs | 0.0375 μs | 0.0351 μs | 2.121 μs | 2.167 μs | 2.255 μs |  1.04 |    0.06 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.650 μs | 0.0338 μs | 0.0316 μs | 2.598 μs | 2.648 μs | 2.703 μs |  1.27 |    0.07 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.032 μs | 0.0393 μs | 0.0436 μs | 1.959 μs | 2.027 μs | 2.115 μs |  0.98 |    0.05 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.242 μs | 0.0390 μs | 0.0365 μs | 2.146 μs | 2.244 μs | 2.288 μs |  1.08 |    0.06 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.776 μs | 0.0552 μs | 0.1164 μs | 2.601 μs | 2.781 μs | 3.043 μs |  1.33 |    0.09 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.001 μs | 0.0324 μs | 0.0270 μs | 5.938 μs | 6.019 μs | 6.024 μs |  1.00 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.398 μs | 0.0378 μs | 0.0335 μs | 6.302 μs | 6.408 μs | 6.425 μs |  1.07 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.153 μs | 0.0372 μs | 0.0311 μs | 7.077 μs | 7.157 μs | 7.196 μs |  1.19 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.508 μs | 0.0446 μs | 0.0396 μs | 6.416 μs | 6.514 μs | 6.572 μs |  1.08 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.946 μs | 0.0106 μs | 0.0094 μs | 5.935 μs | 5.943 μs | 5.968 μs |  0.99 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.651 μs | 0.0328 μs | 0.0274 μs | 5.599 μs | 5.653 μs | 5.706 μs |  0.94 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 21.92 μs | 0.345 μs | 0.288 μs | 21.37 μs | 21.96 μs | 22.47 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 21.75 μs | 0.425 μs | 0.745 μs | 19.82 μs | 21.83 μs | 23.33 μs |  0.99 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 24.75 μs | 0.479 μs | 0.655 μs | 23.86 μs | 24.59 μs | 26.29 μs |  1.13 |    0.03 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 21.00 μs | 0.402 μs | 0.395 μs | 20.08 μs | 20.93 μs | 21.68 μs |  0.96 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.99 μs | 0.436 μs | 0.728 μs | 20.77 μs | 22.04 μs | 23.63 μs |  1.00 |    0.04 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 23.53 μs | 0.315 μs | 0.337 μs | 22.97 μs | 23.52 μs | 24.33 μs |  1.07 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,167.515 μs | 8.6462 μs | 7.2200 μs | 1,154.638 μs | 1,166.140 μs | 1,178.981 μs | 1.000 |    3 | 3.9063 | 1.9531 |   95.2 KB |        1.00 |
| AutoMapperStartup |   246.947 μs | 1.2288 μs | 1.0893 μs |   244.479 μs |   247.263 μs |   248.474 μs | 0.212 |    2 | 5.8594 |      - | 103.82 KB |        1.09 |
| MapsterStartup    |     2.719 μs | 0.0541 μs | 0.0842 μs |     2.598 μs |     2.700 μs |     2.900 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.174 ms | 0.0176 ms | 0.0156 ms | 1.150 ms | 1.181 ms | 1.198 ms |  1.00 |    0.02 |    1 |  5.8594 |  3.9063 |  95.84 KB |        1.00 |
| AutoMapper | 3.533 ms | 0.0562 ms | 0.0525 ms | 3.404 ms | 3.554 ms | 3.583 ms |  3.01 |    0.06 |    3 | 15.6250 |  7.8125 | 309.72 KB |        3.23 |
| Mapster    | 2.630 ms | 0.0084 ms | 0.0070 ms | 2.619 ms | 2.630 ms | 2.644 ms |  2.24 |    0.03 |    2 | 46.8750 | 15.6250 | 766.46 KB |        8.00 |

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

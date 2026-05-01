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
> ⏱ **Last updated:** 2026-05-01 17:11 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.44 ns | **27.13 ns (1.6×)** | 29.65 ns (1.8×) | 83.14 ns (5.1×) | 16.38 ns (1.0×) |
| **Flattening** | 20.21 ns | **30.88 ns (1.5×)** | 37.83 ns (1.9×) | 89.67 ns (4.4×) | 26.12 ns (1.3×) |
| **Deep (2 nested)** | 59.31 ns | **68.96 ns (1.2×)** | 74.47 ns (1.3×) | 126.40 ns (2.1×) | 54.91 ns (0.9×) |
| **Complex (nest+coll)** | 78.94 ns | **100.81 ns (1.3×)** | 97.03 ns (1.2×) | 168.35 ns (2.1×) | 77.51 ns (1.0×) |
| **Collection (100)** | 1.878 μs | **2.065 μs (1.1×)** | 1.859 μs (1.0×) | 2.522 μs (1.3×) | 2.035 μs (1.1×) |
| **Deep Coll (100)** | 5.713 μs | **6.420 μs (1.1×)** | 6.333 μs (1.1×) | 7.030 μs (1.2×) | 5.955 μs (1.0×) |
| **Large Coll (1000)** | 19.33 μs | **18.59 μs (1.0×)** | 18.51 μs (1.0×) | 23.16 μs (1.2×) | 19.36 μs (1.0×) |
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
| Manual               |  16.44 ns | 0.197 ns | 0.175 ns |  16.18 ns |  16.42 ns |  16.79 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.13 ns | 0.297 ns | 0.278 ns |  26.56 ns |  27.23 ns |  27.62 ns |  1.65 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  83.14 ns | 0.239 ns | 0.223 ns |  82.70 ns |  83.13 ns |  83.50 ns |  5.06 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.65 ns | 0.259 ns | 0.229 ns |  29.30 ns |  29.64 ns |  30.16 ns |  1.80 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.38 ns | 0.305 ns | 0.285 ns |  15.97 ns |  16.35 ns |  16.92 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 499.89 ns | 0.863 ns | 0.807 ns | 498.29 ns | 499.94 ns | 501.35 ns | 30.41 |    0.31 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  16.46 ns | 0.153 ns | 0.143 ns |  16.16 ns |  16.45 ns |  16.69 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.44 ns | 0.299 ns | 0.279 ns |  16.13 ns |  16.42 ns |  17.08 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.21 ns | 0.186 ns | 0.174 ns |  19.87 ns |  20.22 ns |  20.48 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.88 ns | 0.259 ns | 0.242 ns |  30.54 ns |  30.85 ns |  31.33 ns |  1.53 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  89.67 ns | 0.355 ns | 0.332 ns |  89.22 ns |  89.57 ns |  90.26 ns |  4.44 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  37.83 ns | 0.446 ns | 0.418 ns |  37.23 ns |  37.69 ns |  38.65 ns |  1.87 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  26.12 ns | 0.279 ns | 0.248 ns |  25.60 ns |  26.13 ns |  26.61 ns |  1.29 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 534.00 ns | 0.955 ns | 0.893 ns | 532.50 ns | 534.15 ns | 535.43 ns | 26.43 |    0.22 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  59.31 ns | 0.821 ns | 0.728 ns |  58.31 ns |  59.19 ns |  60.65 ns |  1.00 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  68.96 ns | 0.731 ns | 0.684 ns |  67.72 ns |  68.75 ns |  70.21 ns |  1.16 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 126.40 ns | 0.941 ns | 0.834 ns | 124.23 ns | 126.32 ns | 127.78 ns |  2.13 |    0.03 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  74.47 ns | 0.697 ns | 0.652 ns |  73.29 ns |  74.47 ns |  75.95 ns |  1.26 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  54.91 ns | 0.534 ns | 0.473 ns |  54.27 ns |  54.85 ns |  55.98 ns |  0.93 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 528.19 ns | 1.095 ns | 0.971 ns | 526.64 ns | 528.24 ns | 530.12 ns |  8.91 |    0.11 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  78.94 ns | 1.645 ns | 1.760 ns |  76.15 ns |  79.39 ns |  81.45 ns |  1.00 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 100.81 ns | 1.358 ns | 1.203 ns |  98.40 ns | 100.68 ns | 102.93 ns |  1.28 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 168.35 ns | 1.693 ns | 1.501 ns | 166.06 ns | 168.54 ns | 171.54 ns |  2.13 |    0.05 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  97.03 ns | 1.516 ns | 1.344 ns |  95.05 ns |  96.92 ns | 100.06 ns |  1.23 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  77.51 ns | 0.631 ns | 0.590 ns |  76.64 ns |  77.39 ns |  78.74 ns |  0.98 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 563.58 ns | 1.223 ns | 1.144 ns | 561.62 ns | 563.63 ns | 565.18 ns |  7.14 |    0.16 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.878 μs | 0.0178 μs | 0.0166 μs | 1.849 μs | 1.878 μs | 1.907 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 2.065 μs | 0.0214 μs | 0.0190 μs | 2.032 μs | 2.063 μs | 2.105 μs |  1.10 |    0.01 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.522 μs | 0.0384 μs | 0.0359 μs | 2.451 μs | 2.529 μs | 2.575 μs |  1.34 |    0.02 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.859 μs | 0.0203 μs | 0.0190 μs | 1.809 μs | 1.859 μs | 1.886 μs |  0.99 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 2.035 μs | 0.0344 μs | 0.0322 μs | 1.949 μs | 2.027 μs | 2.079 μs |  1.08 |    0.02 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.695 μs | 0.0339 μs | 0.0317 μs | 2.642 μs | 2.689 μs | 2.750 μs |  1.43 |    0.02 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.713 μs | 0.0718 μs | 0.0599 μs | 5.621 μs | 5.700 μs | 5.861 μs |  1.00 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.420 μs | 0.0759 μs | 0.0673 μs | 6.222 μs | 6.436 μs | 6.498 μs |  1.12 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.030 μs | 0.0864 μs | 0.0766 μs | 6.917 μs | 7.018 μs | 7.159 μs |  1.23 |    0.02 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.333 μs | 0.0857 μs | 0.0759 μs | 6.185 μs | 6.330 μs | 6.454 μs |  1.11 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.955 μs | 0.1096 μs | 0.1025 μs | 5.707 μs | 5.961 μs | 6.099 μs |  1.04 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.649 μs | 0.0736 μs | 0.0689 μs | 5.528 μs | 5.644 μs | 5.762 μs |  0.99 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.33 μs | 0.302 μs | 0.268 μs | 18.57 μs | 19.35 μs | 19.78 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.59 μs | 0.269 μs | 0.238 μs | 18.05 μs | 18.61 μs | 18.99 μs |  0.96 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 23.16 μs | 0.298 μs | 0.264 μs | 22.63 μs | 23.21 μs | 23.59 μs |  1.20 |    0.02 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.51 μs | 0.157 μs | 0.131 μs | 18.24 μs | 18.50 μs | 18.72 μs |  0.96 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 19.36 μs | 0.222 μs | 0.207 μs | 18.91 μs | 19.39 μs | 19.70 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 25.24 μs | 0.193 μs | 0.180 μs | 24.88 μs | 25.25 μs | 25.54 μs |  1.31 |    0.02 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,224.568 μs | 10.1558 μs | 9.4998 μs | 1,208.151 μs | 1,224.783 μs | 1,242.509 μs | 1.000 |    3 | 5.8594 | 3.9063 |  95.66 KB |        1.00 |
| AutoMapperStartup |   290.608 μs |  1.7610 μs | 1.5611 μs |   289.205 μs |   290.045 μs |   293.887 μs | 0.237 |    2 | 5.8594 |      - | 104.03 KB |        1.09 |
| MapsterStartup    |     2.697 μs |  0.0487 μs | 0.0456 μs |     2.614 μs |     2.685 μs |     2.785 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.170 ms | 0.0107 ms | 0.0095 ms | 1.157 ms | 1.167 ms | 1.191 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.26 KB |        1.00 |
| AutoMapper | 3.260 ms | 0.0103 ms | 0.0092 ms | 3.243 ms | 3.260 ms | 3.280 ms |  2.79 |    0.02 |    3 | 15.6250 |  7.8125 | 310.27 KB |        3.22 |
| Mapster    | 2.540 ms | 0.0110 ms | 0.0097 ms | 2.527 ms | 2.538 ms | 2.564 ms |  2.17 |    0.02 |    2 | 39.0625 | 15.6250 | 764.42 KB |        7.94 |

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

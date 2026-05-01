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
> ⏱ **Last updated:** 2026-05-01 17:04 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 17.57 ns | **28.45 ns (1.6×)** | 28.51 ns (1.6×) | 77.90 ns (4.4×) | 16.38 ns (0.9×) |
| **Flattening** | 20.32 ns | **32.09 ns (1.6×)** | 35.37 ns (1.7×) | 81.81 ns (4.0×) | 26.27 ns (1.3×) |
| **Deep (2 nested)** | 58.12 ns | **70.98 ns (1.2×)** | 72.55 ns (1.2×) | 108.22 ns (1.9×) | 57.82 ns (1.0×) |
| **Complex (nest+coll)** | 81.76 ns | **107.01 ns (1.3×)** | 103.98 ns (1.3×) | 146.82 ns (1.8×) | 82.45 ns (1.0×) |
| **Collection (100)** | 2.089 μs | **2.168 μs (1.0×)** | 2.115 μs (1.0×) | 2.642 μs (1.3×) | 2.181 μs (1.0×) |
| **Deep Coll (100)** | 6.481 μs | **6.746 μs (1.0×)** | 6.639 μs (1.0×) | 7.257 μs (1.1×) | 6.256 μs (1.0×) |
| **Large Coll (1000)** | 22.90 μs | **21.15 μs (0.9×)** | 20.89 μs (0.9×) | 28.80 μs (1.3×) | 23.21 μs (1.0×) |
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
| Manual               |  17.57 ns | 0.192 ns | 0.180 ns |  17.22 ns |  17.56 ns |  17.80 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  28.45 ns | 0.111 ns | 0.093 ns |  28.32 ns |  28.43 ns |  28.60 ns |  1.62 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  77.90 ns | 0.067 ns | 0.063 ns |  77.82 ns |  77.91 ns |  78.02 ns |  4.43 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.51 ns | 0.210 ns | 0.175 ns |  28.20 ns |  28.51 ns |  28.76 ns |  1.62 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.38 ns | 0.031 ns | 0.025 ns |  16.33 ns |  16.38 ns |  16.42 ns |  0.93 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 329.29 ns | 0.486 ns | 0.406 ns | 328.74 ns | 329.38 ns | 330.15 ns | 18.75 |    0.19 |    4 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  17.30 ns | 0.261 ns | 0.231 ns |  16.90 ns |  17.25 ns |  17.71 ns |  0.98 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  17.12 ns | 0.326 ns | 0.305 ns |  16.60 ns |  17.15 ns |  17.54 ns |  0.97 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.32 ns | 0.202 ns | 0.169 ns |  20.07 ns |  20.26 ns |  20.55 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  32.09 ns | 0.626 ns | 0.555 ns |  31.25 ns |  31.96 ns |  32.86 ns |  1.58 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  81.81 ns | 0.275 ns | 0.244 ns |  81.47 ns |  81.74 ns |  82.19 ns |  4.03 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.37 ns | 0.178 ns | 0.158 ns |  35.12 ns |  35.33 ns |  35.64 ns |  1.74 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  26.27 ns | 0.118 ns | 0.099 ns |  26.11 ns |  26.28 ns |  26.41 ns |  1.29 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 330.07 ns | 2.933 ns | 2.744 ns | 325.64 ns | 329.42 ns | 335.12 ns | 16.25 |    0.18 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  58.12 ns | 0.142 ns | 0.119 ns |  57.97 ns |  58.09 ns |  58.29 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  70.98 ns | 0.478 ns | 0.447 ns |  70.36 ns |  71.02 ns |  71.71 ns |  1.22 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 108.22 ns | 0.319 ns | 0.283 ns | 107.88 ns | 108.18 ns | 108.77 ns |  1.86 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| Mapster     |  72.55 ns | 0.156 ns | 0.130 ns |  72.35 ns |  72.52 ns |  72.76 ns |  1.25 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  57.82 ns | 0.766 ns | 0.639 ns |  56.40 ns |  58.02 ns |  58.49 ns |  0.99 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 373.93 ns | 4.559 ns | 4.265 ns | 367.62 ns | 376.11 ns | 378.66 ns |  6.43 |    0.07 |    4 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  81.76 ns | 0.510 ns | 0.452 ns |  80.88 ns |  81.84 ns |  82.41 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 107.01 ns | 1.319 ns | 1.234 ns | 104.05 ns | 107.33 ns | 109.08 ns |  1.31 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 146.82 ns | 0.393 ns | 0.368 ns | 146.13 ns | 146.86 ns | 147.42 ns |  1.80 |    0.01 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     | 103.98 ns | 1.280 ns | 1.069 ns | 102.59 ns | 103.57 ns | 105.90 ns |  1.27 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  82.45 ns | 0.802 ns | 0.750 ns |  81.37 ns |  82.61 ns |  83.83 ns |  1.01 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 406.71 ns | 4.452 ns | 4.165 ns | 401.00 ns | 407.60 ns | 413.21 ns |  4.97 |    0.06 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.089 μs | 0.0270 μs | 0.0253 μs | 2.045 μs | 2.084 μs | 2.133 μs |  1.00 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.168 μs | 0.0424 μs | 0.0566 μs | 1.962 μs | 2.172 μs | 2.245 μs |  1.04 |    0.03 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.642 μs | 0.0521 μs | 0.0640 μs | 2.519 μs | 2.638 μs | 2.753 μs |  1.26 |    0.03 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.115 μs | 0.0152 μs | 0.0135 μs | 2.098 μs | 2.116 μs | 2.139 μs |  1.01 |    0.01 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.181 μs | 0.0192 μs | 0.0170 μs | 2.142 μs | 2.181 μs | 2.203 μs |  1.04 |    0.01 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.737 μs | 0.0526 μs | 0.0541 μs | 2.619 μs | 2.752 μs | 2.796 μs |  1.31 |    0.03 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.481 μs | 0.1185 μs | 0.1365 μs | 6.250 μs | 6.450 μs | 6.746 μs |  1.00 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.746 μs | 0.1108 μs | 0.1037 μs | 6.549 μs | 6.775 μs | 6.885 μs |  1.04 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.257 μs | 0.1030 μs | 0.0964 μs | 7.121 μs | 7.275 μs | 7.425 μs |  1.12 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.639 μs | 0.0360 μs | 0.0319 μs | 6.576 μs | 6.649 μs | 6.682 μs |  1.02 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.256 μs | 0.0607 μs | 0.0538 μs | 6.163 μs | 6.248 μs | 6.330 μs |  0.97 |    0.02 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.614 μs | 0.0339 μs | 0.0317 μs | 5.542 μs | 5.621 μs | 5.665 μs |  0.87 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 22.90 μs | 0.455 μs | 0.487 μs | 22.25 μs | 22.95 μs | 23.69 μs |  1.00 |    0.03 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 21.15 μs | 0.416 μs | 0.750 μs | 18.84 μs | 21.36 μs | 22.04 μs |  0.92 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 28.80 μs | 0.553 μs | 0.659 μs | 27.20 μs | 28.99 μs | 29.62 μs |  1.26 |    0.04 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 20.89 μs | 0.417 μs | 0.557 μs | 19.61 μs | 21.14 μs | 21.47 μs |  0.91 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 23.21 μs | 0.457 μs | 0.685 μs | 22.19 μs | 23.02 μs | 24.67 μs |  1.01 |    0.04 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 24.47 μs | 0.462 μs | 0.432 μs | 23.36 μs | 24.60 μs | 25.01 μs |  1.07 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,218.692 μs | 9.6592 μs | 9.0352 μs | 1,196.022 μs | 1,217.951 μs | 1,229.016 μs | 1.000 |    3 | 5.8594 |      - |   95.5 KB |        1.00 |
| AutoMapperStartup |   239.935 μs | 1.3966 μs | 1.3063 μs |   237.191 μs |   240.163 μs |   242.086 μs | 0.197 |    2 | 5.8594 |      - | 103.96 KB |        1.09 |
| MapsterStartup    |     2.709 μs | 0.0399 μs | 0.0333 μs |     2.665 μs |     2.709 μs |     2.786 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.203 ms | 0.0122 ms | 0.0114 ms | 1.176 ms | 1.205 ms | 1.221 ms |  1.00 |    0.01 |    1 |  5.8594 |       - |   95.5 KB |        1.00 |
| AutoMapper | 3.395 ms | 0.0096 ms | 0.0085 ms | 3.383 ms | 3.393 ms | 3.412 ms |  2.82 |    0.03 |    3 | 15.6250 |  7.8125 | 309.83 KB |        3.24 |
| Mapster    | 2.557 ms | 0.0089 ms | 0.0084 ms | 2.545 ms | 2.555 ms | 2.573 ms |  2.13 |    0.02 |    2 | 39.0625 | 15.6250 | 764.38 KB |        8.00 |

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

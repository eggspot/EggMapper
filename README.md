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
> ⏱ **Last updated:** 2026-05-01 15:11 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.80 ns | **27.92 ns (1.7×)** | 29.48 ns (1.8×) | 78.00 ns (4.6×) | 15.95 ns (0.9×) |
| **Flattening** | 20.14 ns | **32.21 ns (1.6×)** | 34.98 ns (1.7×) | 80.18 ns (4.0×) | 25.09 ns (1.2×) |
| **Deep (2 nested)** | 64.07 ns | **72.71 ns (1.1×)** | 74.90 ns (1.2×) | 110.25 ns (1.7×) | 54.69 ns (0.8×) |
| **Complex (nest+coll)** | 83.32 ns | **108.21 ns (1.3×)** | 103.27 ns (1.2×) | 150.43 ns (1.8×) | 83.02 ns (1.0×) |
| **Collection (100)** | 2.259 μs | **2.385 μs (1.1×)** | 2.323 μs (1.0×) | 2.888 μs (1.3×) | 2.421 μs (1.1×) |
| **Deep Coll (100)** | 6.446 μs | **6.923 μs (1.1×)** | 7.314 μs (1.1×) | 8.230 μs (1.3×) | 6.570 μs (1.0×) |
| **Large Coll (1000)** | 21.06 μs | **20.46 μs (1.0×)** | 21.11 μs (1.0×) | 25.99 μs (1.2×) | 21.31 μs (1.0×) |
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
| Manual               |  16.80 ns | 0.230 ns | 0.215 ns |  16.47 ns |  16.78 ns |  17.11 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.92 ns | 0.516 ns | 0.457 ns |  27.05 ns |  27.88 ns |  28.89 ns |  1.66 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  78.00 ns | 0.156 ns | 0.138 ns |  77.83 ns |  77.98 ns |  78.27 ns |  4.64 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.48 ns | 0.273 ns | 0.256 ns |  29.20 ns |  29.40 ns |  29.98 ns |  1.76 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.95 ns | 0.246 ns | 0.230 ns |  15.60 ns |  16.01 ns |  16.34 ns |  0.95 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 341.74 ns | 1.718 ns | 1.607 ns | 338.55 ns | 341.46 ns | 344.12 ns | 20.35 |    0.27 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  16.33 ns | 0.040 ns | 0.033 ns |  16.26 ns |  16.33 ns |  16.40 ns |  0.97 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.99 ns | 0.225 ns | 0.210 ns |  16.67 ns |  17.02 ns |  17.30 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.14 ns | 0.274 ns | 0.243 ns |  19.79 ns |  20.16 ns |  20.51 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  32.21 ns | 0.165 ns | 0.146 ns |  31.96 ns |  32.22 ns |  32.47 ns |  1.60 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  80.18 ns | 0.264 ns | 0.234 ns |  79.90 ns |  80.12 ns |  80.60 ns |  3.98 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  34.98 ns | 0.301 ns | 0.281 ns |  34.67 ns |  34.90 ns |  35.57 ns |  1.74 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  25.09 ns | 0.137 ns | 0.128 ns |  24.95 ns |  25.02 ns |  25.30 ns |  1.25 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 322.98 ns | 0.769 ns | 0.681 ns | 321.88 ns | 322.79 ns | 324.13 ns | 16.04 |    0.19 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  64.07 ns | 0.265 ns | 0.234 ns |  63.73 ns |  64.03 ns |  64.61 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  72.71 ns | 0.283 ns | 0.265 ns |  72.17 ns |  72.71 ns |  73.23 ns |  1.13 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 110.25 ns | 1.172 ns | 1.097 ns | 108.53 ns | 110.26 ns | 111.85 ns |  1.72 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  74.90 ns | 1.315 ns | 1.292 ns |  73.38 ns |  74.72 ns |  78.13 ns |  1.17 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  54.69 ns | 0.173 ns | 0.144 ns |  54.48 ns |  54.67 ns |  54.90 ns |  0.85 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 365.37 ns | 2.254 ns | 2.109 ns | 360.89 ns | 365.44 ns | 367.94 ns |  5.70 |    0.04 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  83.32 ns | 0.902 ns | 0.800 ns |  81.40 ns |  83.67 ns |  84.24 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 108.21 ns | 1.175 ns | 1.099 ns | 106.56 ns | 108.20 ns | 110.59 ns |  1.30 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 150.43 ns | 0.777 ns | 0.648 ns | 149.33 ns | 150.43 ns | 151.87 ns |  1.81 |    0.02 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     | 103.27 ns | 1.561 ns | 1.460 ns | 100.33 ns | 103.30 ns | 106.25 ns |  1.24 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  83.02 ns | 1.023 ns | 0.957 ns |  81.45 ns |  83.06 ns |  84.92 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 406.14 ns | 1.568 ns | 1.390 ns | 403.97 ns | 405.94 ns | 409.20 ns |  4.87 |    0.05 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.259 μs | 0.0397 μs | 0.0630 μs | 2.095 μs | 2.257 μs | 2.392 μs |  1.00 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.385 μs | 0.0337 μs | 0.0315 μs | 2.343 μs | 2.376 μs | 2.447 μs |  1.06 |    0.03 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.888 μs | 0.0573 μs | 0.0745 μs | 2.761 μs | 2.887 μs | 3.046 μs |  1.28 |    0.05 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.323 μs | 0.0458 μs | 0.0642 μs | 2.195 μs | 2.342 μs | 2.404 μs |  1.03 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.421 μs | 0.0481 μs | 0.0915 μs | 2.228 μs | 2.435 μs | 2.609 μs |  1.07 |    0.05 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.874 μs | 0.0457 μs | 0.0405 μs | 2.788 μs | 2.880 μs | 2.927 μs |  1.27 |    0.04 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.446 μs | 0.1252 μs | 0.1110 μs | 6.333 μs | 6.395 μs | 6.702 μs |  1.00 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.923 μs | 0.1342 μs | 0.2278 μs | 6.643 μs | 6.828 μs | 7.471 μs |  1.07 |    0.04 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 8.230 μs | 0.1387 μs | 0.1297 μs | 7.997 μs | 8.250 μs | 8.387 μs |  1.28 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 7.314 μs | 0.1409 μs | 0.1929 μs | 7.002 μs | 7.292 μs | 7.760 μs |  1.13 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.570 μs | 0.0755 μs | 0.0590 μs | 6.466 μs | 6.566 μs | 6.653 μs |  1.02 |    0.02 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 6.057 μs | 0.0882 μs | 0.0825 μs | 5.884 μs | 6.062 μs | 6.196 μs |  0.94 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 21.06 μs | 0.415 μs | 0.780 μs | 19.68 μs | 21.13 μs | 22.42 μs |  1.00 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 20.46 μs | 0.409 μs | 0.624 μs | 19.59 μs | 20.23 μs | 21.43 μs |  0.97 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 25.99 μs | 0.486 μs | 0.477 μs | 25.21 μs | 25.99 μs | 26.69 μs |  1.24 |    0.05 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 21.11 μs | 0.415 μs | 0.568 μs | 20.20 μs | 21.23 μs | 22.01 μs |  1.00 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.31 μs | 0.425 μs | 0.636 μs | 20.22 μs | 21.40 μs | 22.61 μs |  1.01 |    0.05 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 22.15 μs | 0.195 μs | 0.182 μs | 21.89 μs | 22.08 μs | 22.51 μs |  1.05 |    0.04 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,179.359 μs | 6.7682 μs | 5.9998 μs | 1,169.365 μs | 1,179.438 μs | 1,192.356 μs | 1.000 |    3 | 5.8594 | 3.9063 |   95.9 KB |        1.00 |
| AutoMapperStartup |   241.670 μs | 1.2488 μs | 1.0428 μs |   240.499 μs |   241.560 μs |   244.013 μs | 0.205 |    2 | 5.8594 |      - |  104.2 KB |        1.09 |
| MapsterStartup    |     2.597 μs | 0.0267 μs | 0.0250 μs |     2.526 μs |     2.606 μs |     2.623 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.186 ms | 0.0121 ms | 0.0107 ms | 1.172 ms | 1.183 ms | 1.204 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  95.94 KB |        1.00 |
| AutoMapper | 3.428 ms | 0.0078 ms | 0.0065 ms | 3.419 ms | 3.429 ms | 3.438 ms |  2.89 |    0.03 |    3 | 15.6250 |  7.8125 | 309.96 KB |        3.23 |
| Mapster    | 2.602 ms | 0.0248 ms | 0.0220 ms | 2.578 ms | 2.595 ms | 2.647 ms |  2.19 |    0.03 |    2 | 39.0625 | 15.6250 | 757.39 KB |        7.89 |

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

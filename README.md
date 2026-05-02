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
> ⏱ **Last updated:** 2026-05-02 03:40 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 14.86 ns | **28.39 ns (1.9×)** | 28.18 ns (1.9×) | 80.17 ns (5.4×) | 14.99 ns (1.0×) |
| **Flattening** | 18.70 ns | **29.25 ns (1.6×)** | 35.68 ns (1.9×) | 86.84 ns (4.6×) | 23.79 ns (1.3×) |
| **Deep (2 nested)** | 54.00 ns | **64.26 ns (1.2×)** | 68.33 ns (1.3×) | 119.40 ns (2.2×) | 49.61 ns (0.9×) |
| **Complex (nest+coll)** | 70.69 ns | **92.57 ns (1.3×)** | 89.81 ns (1.3×) | 147.81 ns (2.1×) | 70.99 ns (1.0×) |
| **Collection (100)** | 1.757 μs | **1.659 μs (0.9×)** | 1.736 μs (1.0×) | 2.315 μs (1.3×) | 1.839 μs (1.1×) |
| **Deep Coll (100)** | 5.315 μs | **5.615 μs (1.1×)** | 5.764 μs (1.1×) | 6.386 μs (1.2×) | 6.063 μs (1.1×) |
| **Large Coll (1000)** | 17.47 μs | **16.86 μs (1.0×)** | 19.75 μs (1.1×) | 21.48 μs (1.2×) | 18.18 μs (1.0×) |
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
| Manual               |  14.86 ns | 0.052 ns | 0.049 ns |  14.79 ns |  14.84 ns |  14.94 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  28.39 ns | 0.261 ns | 0.244 ns |  28.01 ns |  28.45 ns |  28.77 ns |  1.91 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  80.17 ns | 0.184 ns | 0.163 ns |  79.93 ns |  80.14 ns |  80.54 ns |  5.40 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.18 ns | 0.109 ns | 0.101 ns |  27.94 ns |  28.19 ns |  28.33 ns |  1.90 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  14.99 ns | 0.061 ns | 0.057 ns |  14.89 ns |  15.01 ns |  15.07 ns |  1.01 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 483.69 ns | 0.665 ns | 0.590 ns | 482.46 ns | 483.59 ns | 484.79 ns | 32.56 |    0.11 |    4 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  14.99 ns | 0.131 ns | 0.123 ns |  14.73 ns |  15.02 ns |  15.14 ns |  1.01 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.34 ns | 0.082 ns | 0.076 ns |  15.19 ns |  15.35 ns |  15.45 ns |  1.03 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.70 ns | 0.094 ns | 0.088 ns |  18.56 ns |  18.70 ns |  18.92 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.25 ns | 0.177 ns | 0.166 ns |  28.95 ns |  29.32 ns |  29.46 ns |  1.56 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  86.84 ns | 0.209 ns | 0.163 ns |  86.49 ns |  86.88 ns |  87.10 ns |  4.64 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.68 ns | 0.105 ns | 0.099 ns |  35.43 ns |  35.68 ns |  35.79 ns |  1.91 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.79 ns | 0.143 ns | 0.127 ns |  23.49 ns |  23.78 ns |  23.97 ns |  1.27 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 533.86 ns | 1.894 ns | 1.772 ns | 530.65 ns | 534.49 ns | 536.34 ns | 28.55 |    0.16 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  54.00 ns | 0.391 ns | 0.365 ns |  53.28 ns |  54.10 ns |  54.68 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.26 ns | 0.262 ns | 0.219 ns |  63.71 ns |  64.31 ns |  64.56 ns |  1.19 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 119.40 ns | 0.402 ns | 0.356 ns | 118.78 ns | 119.36 ns | 119.89 ns |  2.21 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  68.33 ns | 0.262 ns | 0.245 ns |  67.78 ns |  68.34 ns |  68.76 ns |  1.27 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.61 ns | 0.396 ns | 0.351 ns |  49.19 ns |  49.61 ns |  50.37 ns |  0.92 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 506.73 ns | 1.021 ns | 0.905 ns | 504.88 ns | 506.91 ns | 508.19 ns |  9.39 |    0.06 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  70.69 ns | 0.309 ns | 0.289 ns |  69.96 ns |  70.66 ns |  71.21 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  92.57 ns | 0.403 ns | 0.336 ns |  91.72 ns |  92.69 ns |  92.94 ns |  1.31 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 147.81 ns | 0.570 ns | 0.505 ns | 147.11 ns | 147.73 ns | 148.64 ns |  2.09 |    0.01 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  89.81 ns | 0.518 ns | 0.484 ns |  88.48 ns |  89.89 ns |  90.42 ns |  1.27 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.99 ns | 0.448 ns | 0.419 ns |  70.20 ns |  70.95 ns |  71.66 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 550.25 ns | 1.471 ns | 1.228 ns | 547.99 ns | 550.18 ns | 552.42 ns |  7.78 |    0.04 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.757 μs | 0.0139 μs | 0.0123 μs | 1.735 μs | 1.757 μs | 1.776 μs |  1.00 |    0.01 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.659 μs | 0.0183 μs | 0.0171 μs | 1.639 μs | 1.655 μs | 1.689 μs |  0.94 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.315 μs | 0.0111 μs | 0.0098 μs | 2.294 μs | 2.315 μs | 2.331 μs |  1.32 |    0.01 |    4 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.736 μs | 0.0125 μs | 0.0117 μs | 1.720 μs | 1.733 μs | 1.760 μs |  0.99 |    0.01 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.839 μs | 0.0068 μs | 0.0060 μs | 1.826 μs | 1.839 μs | 1.851 μs |  1.05 |    0.01 |    3 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.531 μs | 0.0256 μs | 0.0239 μs | 2.499 μs | 2.520 μs | 2.580 μs |  1.44 |    0.02 |    5 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.315 μs | 0.0282 μs | 0.0250 μs | 5.254 μs | 5.315 μs | 5.355 μs |  1.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.615 μs | 0.0306 μs | 0.0286 μs | 5.557 μs | 5.617 μs | 5.661 μs |  1.06 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.386 μs | 0.0279 μs | 0.0247 μs | 6.336 μs | 6.387 μs | 6.437 μs |  1.20 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.764 μs | 0.0229 μs | 0.0191 μs | 5.739 μs | 5.760 μs | 5.812 μs |  1.08 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.063 μs | 0.0217 μs | 0.0203 μs | 5.994 μs | 6.069 μs | 6.081 μs |  1.14 |    3 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.191 μs | 0.0406 μs | 0.0360 μs | 5.123 μs | 5.195 μs | 5.256 μs |  0.98 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.47 μs | 0.118 μs | 0.110 μs | 17.19 μs | 17.45 μs | 17.66 μs |  1.00 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.86 μs | 0.114 μs | 0.101 μs | 16.64 μs | 16.87 μs | 17.03 μs |  0.97 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.48 μs | 0.130 μs | 0.121 μs | 21.14 μs | 21.48 μs | 21.65 μs |  1.23 |    6 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 19.75 μs | 0.119 μs | 0.105 μs | 19.59 μs | 19.76 μs | 19.93 μs |  1.13 |    4 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.18 μs | 0.128 μs | 0.113 μs | 17.97 μs | 18.16 μs | 18.38 μs |  1.04 |    3 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.40 μs | 0.064 μs | 0.060 μs | 20.31 μs | 20.38 μs | 20.50 μs |  1.17 |    5 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,229.132 μs | 8.8240 μs | 8.2540 μs | 1,218.675 μs | 1,228.661 μs | 1,242.541 μs | 1.000 |    3 | 5.8594 | 3.9063 |  95.71 KB |        1.00 |
| AutoMapperStartup |   283.317 μs | 3.2149 μs | 2.6846 μs |   280.181 μs |   282.107 μs |   288.725 μs | 0.231 |    2 | 5.8594 |      - | 103.88 KB |        1.09 |
| MapsterStartup    |     2.451 μs | 0.0123 μs | 0.0115 μs |     2.426 μs |     2.456 μs |     2.465 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.177 ms | 0.0068 ms | 0.0060 ms | 1.166 ms | 1.177 ms | 1.188 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.25 KB |        1.00 |
| AutoMapper | 3.269 ms | 0.0180 ms | 0.0160 ms | 3.246 ms | 3.268 ms | 3.305 ms |  2.78 |    0.02 |    3 | 15.6250 |  7.8125 | 310.07 KB |        3.22 |
| Mapster    | 2.506 ms | 0.0272 ms | 0.0254 ms | 2.484 ms | 2.490 ms | 2.556 ms |  2.13 |    0.02 |    2 | 39.0625 | 15.6250 | 759.95 KB |        7.90 |

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

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
> ⏱ **Last updated:** 2026-05-02 09:11 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.37 ns | **25.64 ns (1.7×)** | 27.60 ns (1.8×) | 80.35 ns (5.2×) | 15.12 ns (1.0×) |
| **Flattening** | 21.21 ns | **31.52 ns (1.5×)** | 38.02 ns (1.8×) | 90.42 ns (4.3×) | 24.87 ns (1.2×) |
| **Deep (2 nested)** | 53.77 ns | **68.07 ns (1.3×)** | 69.19 ns (1.3×) | 124.16 ns (2.3×) | 51.77 ns (1.0×) |
| **Complex (nest+coll)** | 72.11 ns | **94.90 ns (1.3×)** | 90.51 ns (1.3×) | 150.54 ns (2.1×) | 70.00 ns (1.0×) |
| **Collection (100)** | 1.699 μs | **1.741 μs (1.0×)** | 1.727 μs (1.0×) | 2.329 μs (1.4×) | 1.845 μs (1.1×) |
| **Deep Coll (100)** | 5.314 μs | **5.721 μs (1.1×)** | 5.736 μs (1.1×) | 6.354 μs (1.2×) | 5.279 μs (1.0×) |
| **Large Coll (1000)** | 18.38 μs | **17.81 μs (1.0×)** | 17.70 μs (1.0×) | 21.76 μs (1.2×) | 18.37 μs (1.0×) |
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
| Manual               |  15.37 ns | 0.313 ns | 0.277 ns |  14.97 ns |  15.43 ns |  15.86 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  25.64 ns | 0.490 ns | 0.565 ns |  24.90 ns |  25.79 ns |  26.62 ns |  1.67 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  80.35 ns | 0.292 ns | 0.259 ns |  80.05 ns |  80.28 ns |  81.02 ns |  5.23 |    0.09 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.60 ns | 0.072 ns | 0.068 ns |  27.48 ns |  27.60 ns |  27.73 ns |  1.80 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.12 ns | 0.357 ns | 0.316 ns |  14.80 ns |  14.99 ns |  15.77 ns |  0.98 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 494.25 ns | 1.868 ns | 1.656 ns | 491.66 ns | 494.09 ns | 497.50 ns | 32.17 |    0.57 |    6 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.14 ns | 0.199 ns | 0.176 ns |  14.90 ns |  15.11 ns |  15.56 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  17.87 ns | 0.188 ns | 0.157 ns |  17.56 ns |  17.88 ns |  18.13 ns |  1.16 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  21.21 ns | 0.310 ns | 0.290 ns |  20.65 ns |  21.13 ns |  21.77 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.52 ns | 0.194 ns | 0.181 ns |  31.30 ns |  31.46 ns |  31.83 ns |  1.49 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  90.42 ns | 1.284 ns | 1.201 ns |  88.51 ns |  90.36 ns |  92.12 ns |  4.26 |    0.08 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  38.02 ns | 0.704 ns | 0.658 ns |  36.97 ns |  37.84 ns |  38.83 ns |  1.79 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.87 ns | 0.405 ns | 0.379 ns |  24.16 ns |  24.90 ns |  25.50 ns |  1.17 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 520.26 ns | 1.532 ns | 1.433 ns | 517.43 ns | 520.38 ns | 522.31 ns | 24.54 |    0.33 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.77 ns | 0.110 ns | 0.103 ns |  53.63 ns |  53.79 ns |  53.96 ns |  1.00 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  68.07 ns | 1.342 ns | 1.378 ns |  65.63 ns |  68.00 ns |  69.88 ns |  1.27 |    0.03 |    2 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 124.16 ns | 1.191 ns | 0.930 ns | 123.41 ns | 123.75 ns | 126.34 ns |  2.31 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| Mapster     |  69.19 ns | 0.702 ns | 0.586 ns |  68.34 ns |  69.00 ns |  70.06 ns |  1.29 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  51.77 ns | 0.948 ns | 0.887 ns |  50.57 ns |  52.03 ns |  53.25 ns |  0.96 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 495.27 ns | 5.841 ns | 5.464 ns | 489.84 ns | 493.76 ns | 506.81 ns |  9.21 |    0.10 |    4 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  72.11 ns | 1.468 ns | 1.301 ns |  70.84 ns |  71.75 ns |  75.01 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  94.90 ns | 1.843 ns | 2.194 ns |  91.35 ns |  95.10 ns |  99.45 ns |  1.32 |    0.04 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 150.54 ns | 2.178 ns | 1.930 ns | 148.69 ns | 149.97 ns | 155.43 ns |  2.09 |    0.04 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  90.51 ns | 0.808 ns | 0.756 ns |  89.51 ns |  90.52 ns |  92.04 ns |  1.26 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.00 ns | 0.181 ns | 0.142 ns |  69.82 ns |  70.01 ns |  70.33 ns |  0.97 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 545.76 ns | 1.424 ns | 1.262 ns | 543.80 ns | 545.71 ns | 548.14 ns |  7.57 |    0.13 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.699 μs | 0.0083 μs | 0.0074 μs | 1.690 μs | 1.698 μs | 1.715 μs |  1.00 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.741 μs | 0.0102 μs | 0.0096 μs | 1.716 μs | 1.741 μs | 1.751 μs |  1.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.329 μs | 0.0142 μs | 0.0133 μs | 2.309 μs | 2.331 μs | 2.352 μs |  1.37 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.727 μs | 0.0059 μs | 0.0049 μs | 1.718 μs | 1.727 μs | 1.735 μs |  1.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.845 μs | 0.0080 μs | 0.0074 μs | 1.831 μs | 1.845 μs | 1.858 μs |  1.09 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.468 μs | 0.0079 μs | 0.0066 μs | 2.456 μs | 2.470 μs | 2.477 μs |  1.45 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.314 μs | 0.0159 μs | 0.0149 μs | 5.293 μs | 5.308 μs | 5.344 μs |  1.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.721 μs | 0.0414 μs | 0.0323 μs | 5.656 μs | 5.723 μs | 5.759 μs |  1.08 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.354 μs | 0.0201 μs | 0.0167 μs | 6.323 μs | 6.358 μs | 6.382 μs |  1.20 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.736 μs | 0.0196 μs | 0.0184 μs | 5.693 μs | 5.735 μs | 5.758 μs |  1.08 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.279 μs | 0.0146 μs | 0.0129 μs | 5.263 μs | 5.280 μs | 5.303 μs |  0.99 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.163 μs | 0.0084 μs | 0.0070 μs | 5.152 μs | 5.163 μs | 5.173 μs |  0.97 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.38 μs | 0.227 μs | 0.202 μs | 18.02 μs | 18.39 μs | 18.68 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.81 μs | 0.150 μs | 0.140 μs | 17.56 μs | 17.83 μs | 18.01 μs |  0.97 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.76 μs | 0.173 μs | 0.162 μs | 21.45 μs | 21.77 μs | 21.98 μs |  1.18 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.70 μs | 0.296 μs | 0.277 μs | 17.21 μs | 17.71 μs | 18.18 μs |  0.96 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.37 μs | 0.120 μs | 0.106 μs | 18.13 μs | 18.36 μs | 18.58 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.61 μs | 0.092 μs | 0.082 μs | 20.47 μs | 20.61 μs | 20.78 μs |  1.12 |    0.01 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,171.720 μs | 5.2918 μs | 4.6910 μs | 1,165.771 μs | 1,170.596 μs | 1,181.879 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.31 KB |        1.00 |
| AutoMapperStartup |   286.611 μs | 5.0781 μs | 4.7501 μs |   282.188 μs |   284.012 μs |   294.875 μs | 0.245 |    2 | 5.8594 |      - | 103.82 KB |        1.09 |
| MapsterStartup    |     2.510 μs | 0.0218 μs | 0.0194 μs |     2.466 μs |     2.513 μs |     2.538 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.180 ms | 0.0075 ms | 0.0063 ms | 1.171 ms | 1.181 ms | 1.194 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.43 KB |        1.00 |
| AutoMapper | 3.251 ms | 0.0099 ms | 0.0083 ms | 3.238 ms | 3.252 ms | 3.266 ms |  2.75 |    0.02 |    3 | 15.6250 |  7.8125 | 310.83 KB |        3.22 |
| Mapster    | 2.503 ms | 0.0107 ms | 0.0095 ms | 2.491 ms | 2.503 ms | 2.523 ms |  2.12 |    0.01 |    2 | 39.0625 | 15.6250 | 757.54 KB |        7.86 |

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

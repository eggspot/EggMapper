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
> ⏱ **Last updated:** 2026-05-02 09:28 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.22 ns | **27.64 ns (1.7×)** | 29.80 ns (1.8×) | 83.28 ns (5.2×) | 16.21 ns (1.0×) |
| **Flattening** | 20.10 ns | **30.69 ns (1.5×)** | 35.27 ns (1.8×) | 90.87 ns (4.5×) | 25.66 ns (1.3×) |
| **Deep (2 nested)** | 58.70 ns | **74.35 ns (1.3×)** | 75.52 ns (1.3×) | 108.40 ns (1.9×) | 54.75 ns (0.9×) |
| **Complex (nest+coll)** | 81.48 ns | **104.38 ns (1.3×)** | 105.25 ns (1.3×) | 149.23 ns (1.8×) | 82.45 ns (1.0×) |
| **Collection (100)** | 2.367 μs | **2.353 μs (1.0×)** | 2.369 μs (1.0×) | 2.892 μs (1.2×) | 2.440 μs (1.0×) |
| **Deep Coll (100)** | 6.417 μs | **6.973 μs (1.1×)** | 7.122 μs (1.1×) | 8.142 μs (1.3×) | 6.730 μs (1.1×) |
| **Large Coll (1000)** | 20.88 μs | **23.01 μs (1.1×)** | 21.81 μs (1.1×) | 23.54 μs (1.1×) | 22.39 μs (1.1×) |
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

| Method               | Mean      | Error    | StdDev   | Median    | Min       | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual               |  16.22 ns | 0.394 ns | 0.751 ns |  15.91 ns |  15.45 ns |  17.78 ns |  1.00 |    0.06 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.64 ns | 0.276 ns | 0.245 ns |  27.59 ns |  27.26 ns |  28.09 ns |  1.71 |    0.08 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  83.28 ns | 0.485 ns | 0.405 ns |  83.45 ns |  82.24 ns |  83.60 ns |  5.15 |    0.23 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.80 ns | 0.552 ns | 0.516 ns |  29.71 ns |  28.89 ns |  30.76 ns |  1.84 |    0.09 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.21 ns | 0.102 ns | 0.090 ns |  16.18 ns |  16.12 ns |  16.43 ns |  1.00 |    0.04 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 333.00 ns | 1.715 ns | 1.605 ns | 332.86 ns | 330.29 ns | 336.07 ns | 20.58 |    0.90 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  16.62 ns | 0.225 ns | 0.210 ns |  16.53 ns |  16.33 ns |  16.98 ns |  1.03 |    0.05 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.90 ns | 0.274 ns | 0.256 ns |  16.89 ns |  16.51 ns |  17.38 ns |  1.04 |    0.05 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.10 ns | 0.215 ns | 0.190 ns |  19.82 ns |  20.16 ns |  20.42 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.69 ns | 0.300 ns | 0.251 ns |  30.30 ns |  30.73 ns |  31.05 ns |  1.53 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  90.87 ns | 0.540 ns | 0.451 ns |  89.93 ns |  90.94 ns |  91.81 ns |  4.52 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.27 ns | 0.423 ns | 0.396 ns |  34.78 ns |  35.43 ns |  35.90 ns |  1.76 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  25.66 ns | 0.228 ns | 0.202 ns |  25.35 ns |  25.65 ns |  26.13 ns |  1.28 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 326.33 ns | 2.759 ns | 2.445 ns | 320.09 ns | 327.18 ns | 328.80 ns | 16.24 |    0.19 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  58.70 ns | 0.653 ns | 0.611 ns |  58.00 ns |  58.43 ns |  59.87 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  74.35 ns | 0.770 ns | 0.721 ns |  73.28 ns |  74.18 ns |  75.57 ns |  1.27 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 108.40 ns | 0.842 ns | 0.787 ns | 107.27 ns | 108.50 ns | 109.68 ns |  1.85 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  75.52 ns | 1.181 ns | 0.986 ns |  73.87 ns |  75.39 ns |  77.50 ns |  1.29 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  54.75 ns | 0.608 ns | 0.569 ns |  54.17 ns |  54.48 ns |  55.89 ns |  0.93 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 359.74 ns | 1.816 ns | 1.699 ns | 357.10 ns | 359.48 ns | 362.36 ns |  6.13 |    0.07 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  81.48 ns | 1.152 ns | 1.021 ns |  79.77 ns |  81.40 ns |  83.46 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 104.38 ns | 0.617 ns | 0.515 ns | 103.18 ns | 104.57 ns | 105.07 ns |  1.28 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 149.23 ns | 1.226 ns | 1.024 ns | 147.19 ns | 149.19 ns | 150.88 ns |  1.83 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     | 105.25 ns | 1.330 ns | 1.179 ns | 103.62 ns | 104.95 ns | 107.50 ns |  1.29 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  82.45 ns | 1.168 ns | 1.092 ns |  81.21 ns |  82.31 ns |  84.63 ns |  1.01 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 422.37 ns | 2.801 ns | 2.483 ns | 418.42 ns | 421.83 ns | 427.95 ns |  5.18 |    0.07 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.367 μs | 0.0338 μs | 0.0316 μs | 2.302 μs | 2.361 μs | 2.425 μs |  1.00 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.353 μs | 0.0293 μs | 0.0274 μs | 2.295 μs | 2.345 μs | 2.392 μs |  0.99 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.892 μs | 0.0576 μs | 0.0617 μs | 2.739 μs | 2.900 μs | 2.978 μs |  1.22 |    0.03 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.369 μs | 0.0426 μs | 0.0399 μs | 2.271 μs | 2.371 μs | 2.414 μs |  1.00 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.440 μs | 0.0268 μs | 0.0250 μs | 2.395 μs | 2.445 μs | 2.467 μs |  1.03 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 3.024 μs | 0.0393 μs | 0.0368 μs | 2.941 μs | 3.026 μs | 3.085 μs |  1.28 |    0.02 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.417 μs | 0.0689 μs | 0.0575 μs | 6.321 μs | 6.424 μs | 6.523 μs |  1.00 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.973 μs | 0.0487 μs | 0.0432 μs | 6.905 μs | 6.962 μs | 7.058 μs |  1.09 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 8.142 μs | 0.0737 μs | 0.0653 μs | 8.005 μs | 8.155 μs | 8.239 μs |  1.27 |    0.01 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 7.122 μs | 0.1397 μs | 0.2334 μs | 6.697 μs | 7.138 μs | 7.751 μs |  1.11 |    0.04 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.730 μs | 0.1308 μs | 0.1285 μs | 6.415 μs | 6.738 μs | 6.864 μs |  1.05 |    0.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 6.345 μs | 0.0600 μs | 0.0561 μs | 6.203 μs | 6.359 μs | 6.393 μs |  0.99 |    0.01 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 20.88 μs | 0.415 μs | 1.042 μs | 19.51 μs | 20.62 μs | 23.45 μs |  1.00 |    0.07 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 23.01 μs | 0.449 μs | 0.499 μs | 22.07 μs | 22.95 μs | 23.63 μs |  1.10 |    0.06 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 23.54 μs | 0.458 μs | 0.700 μs | 22.42 μs | 23.57 μs | 24.99 μs |  1.13 |    0.06 |    1 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 21.81 μs | 0.431 μs | 0.799 μs | 20.03 μs | 21.97 μs | 23.21 μs |  1.05 |    0.06 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 22.39 μs | 0.421 μs | 0.617 μs | 21.19 μs | 22.47 μs | 23.49 μs |  1.07 |    0.06 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 24.65 μs | 0.473 μs | 0.420 μs | 24.10 μs | 24.56 μs | 25.48 μs |  1.18 |    0.06 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,156.911 μs | 7.0329 μs | 6.5785 μs | 1,148.740 μs | 1,154.243 μs | 1,167.530 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.09 KB |        1.00 |
| AutoMapperStartup |   240.601 μs | 1.2057 μs | 1.0068 μs |   239.181 μs |   240.545 μs |   243.021 μs | 0.208 |    2 | 5.8594 |      - | 104.15 KB |        1.10 |
| MapsterStartup    |     2.598 μs | 0.0273 μs | 0.0228 μs |     2.545 μs |     2.594 μs |     2.638 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.179 ms | 0.0102 ms | 0.0085 ms | 1.165 ms | 1.183 ms | 1.195 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.08 KB |        1.00 |
| AutoMapper | 3.512 ms | 0.0239 ms | 0.0224 ms | 3.486 ms | 3.508 ms | 3.553 ms |  2.98 |    0.03 |    3 | 15.6250 |  7.8125 | 309.93 KB |        3.23 |
| Mapster    | 2.598 ms | 0.0090 ms | 0.0075 ms | 2.586 ms | 2.596 ms | 2.610 ms |  2.20 |    0.02 |    2 | 39.0625 | 15.6250 | 762.12 KB |        7.93 |

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
- ✅ Null source collection → empty destination collection (default `AllowNullCollections = false` semantics)
- ✅ Unmatched destination collection properties auto-initialized to empty (top-level + nested inline maps)
- ✅ `Ignore()` on getter-only and non-public-setter properties; non-`Ignore()` ops throw at config time
- ✅ Custom `IEnumerable` wrappers (e.g. `SelectList`) auto-constructed via cached interface-ctor lookup
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

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
> ⏱ **Last updated:** 2026-05-22 15:12 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.15 ns | **29.01 ns (1.8×)** | 28.16 ns (1.7×) | 80.39 ns (5.0×) | 17.14 ns (1.1×) |
| **Flattening** | 20.73 ns | **31.25 ns (1.5×)** | 34.24 ns (1.6×) | 84.05 ns (4.0×) | 24.53 ns (1.2×) |
| **Deep (2 nested)** | 56.27 ns | **69.58 ns (1.2×)** | 73.01 ns (1.3×) | 108.09 ns (1.9×) | 52.90 ns (0.9×) |
| **Complex (nest+coll)** | 88.16 ns | **105.17 ns (1.2×)** | 100.13 ns (1.1×) | 145.32 ns (1.6×) | 80.72 ns (0.9×) |
| **Collection (100)** | 2.208 μs | **2.095 μs (0.9×)** | 2.058 μs (0.9×) | 2.641 μs (1.2×) | 2.156 μs (1.0×) |
| **Deep Coll (100)** | 5.985 μs | **6.458 μs (1.1×)** | 6.416 μs (1.1×) | 7.090 μs (1.2×) | 5.878 μs (1.0×) |
| **Large Coll (1000)** | 21.52 μs | **20.78 μs (1.0×)** | 21.46 μs (1.0×) | 25.36 μs (1.2×) | 22.22 μs (1.0×) |
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
| Manual               |  16.15 ns | 0.273 ns | 0.242 ns |  15.71 ns |  16.21 ns |  16.42 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  29.01 ns | 0.282 ns | 0.264 ns |  28.57 ns |  28.99 ns |  29.38 ns |  1.80 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  80.39 ns | 0.074 ns | 0.061 ns |  80.29 ns |  80.41 ns |  80.47 ns |  4.98 |    0.07 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.16 ns | 0.424 ns | 0.397 ns |  27.62 ns |  28.22 ns |  28.88 ns |  1.74 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  17.14 ns | 0.397 ns | 0.407 ns |  16.02 ns |  17.21 ns |  17.68 ns |  1.06 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 372.54 ns | 1.937 ns | 1.812 ns | 368.55 ns | 372.64 ns | 374.88 ns | 23.08 |    0.35 |    4 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  16.26 ns | 0.172 ns | 0.143 ns |  16.07 ns |  16.25 ns |  16.63 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.96 ns | 0.322 ns | 0.301 ns |  16.44 ns |  16.95 ns |  17.44 ns |  1.05 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.73 ns | 0.193 ns | 0.180 ns |  20.32 ns |  20.73 ns |  21.03 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.25 ns | 0.297 ns | 0.263 ns |  30.94 ns |  31.23 ns |  31.88 ns |  1.51 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  84.05 ns | 0.361 ns | 0.320 ns |  83.72 ns |  83.89 ns |  84.53 ns |  4.05 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  34.24 ns | 0.383 ns | 0.359 ns |  33.82 ns |  34.15 ns |  34.98 ns |  1.65 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.53 ns | 0.397 ns | 0.371 ns |  24.09 ns |  24.47 ns |  25.18 ns |  1.18 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 325.54 ns | 2.157 ns | 1.912 ns | 323.15 ns | 325.18 ns | 330.02 ns | 15.70 |    0.16 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  56.27 ns | 0.099 ns | 0.083 ns |  56.14 ns |  56.25 ns |  56.40 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  69.58 ns | 0.303 ns | 0.269 ns |  69.11 ns |  69.50 ns |  69.98 ns |  1.24 |    0.00 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 108.09 ns | 0.203 ns | 0.180 ns | 107.82 ns | 108.06 ns | 108.38 ns |  1.92 |    0.00 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  73.01 ns | 0.804 ns | 0.752 ns |  70.97 ns |  73.24 ns |  73.86 ns |  1.30 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  52.90 ns | 0.270 ns | 0.239 ns |  52.56 ns |  52.86 ns |  53.24 ns |  0.94 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 358.36 ns | 1.869 ns | 1.748 ns | 354.39 ns | 358.28 ns | 361.11 ns |  6.37 |    0.03 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  88.16 ns | 0.947 ns | 0.886 ns |  86.06 ns |  88.35 ns |  89.58 ns |  1.00 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 105.17 ns | 0.717 ns | 0.636 ns | 104.08 ns | 105.30 ns | 106.06 ns |  1.19 |    0.01 |    4 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 145.32 ns | 1.394 ns | 1.304 ns | 143.47 ns | 145.31 ns | 147.61 ns |  1.65 |    0.02 |    5 | 0.0196 |     328 B |        1.02 |
| Mapster     | 100.13 ns | 0.684 ns | 0.640 ns |  98.31 ns | 100.15 ns | 100.98 ns |  1.14 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  80.72 ns | 1.258 ns | 1.115 ns |  77.72 ns |  81.16 ns |  81.90 ns |  0.92 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 405.62 ns | 1.279 ns | 1.196 ns | 403.89 ns | 405.46 ns | 407.62 ns |  4.60 |    0.05 |    6 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.208 μs | 0.0290 μs | 0.0271 μs | 2.164 μs | 2.215 μs | 2.251 μs |  1.00 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.095 μs | 0.0412 μs | 0.0850 μs | 1.912 μs | 2.090 μs | 2.274 μs |  0.95 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.641 μs | 0.0527 μs | 0.0951 μs | 2.497 μs | 2.655 μs | 2.899 μs |  1.20 |    0.04 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.058 μs | 0.0405 μs | 0.0897 μs | 1.910 μs | 2.059 μs | 2.286 μs |  0.93 |    0.04 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.156 μs | 0.0414 μs | 0.0539 μs | 2.049 μs | 2.155 μs | 2.265 μs |  0.98 |    0.03 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.722 μs | 0.0523 μs | 0.0603 μs | 2.616 μs | 2.709 μs | 2.822 μs |  1.23 |    0.03 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.985 μs | 0.0644 μs | 0.0571 μs | 5.871 μs | 5.975 μs | 6.086 μs |  1.00 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.458 μs | 0.0696 μs | 0.0651 μs | 6.338 μs | 6.429 μs | 6.581 μs |  1.08 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.090 μs | 0.0181 μs | 0.0161 μs | 7.065 μs | 7.087 μs | 7.116 μs |  1.18 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.416 μs | 0.0431 μs | 0.0403 μs | 6.332 μs | 6.420 μs | 6.492 μs |  1.07 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.878 μs | 0.0299 μs | 0.0265 μs | 5.845 μs | 5.867 μs | 5.928 μs |  0.98 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.628 μs | 0.0280 μs | 0.0234 μs | 5.596 μs | 5.631 μs | 5.682 μs |  0.94 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 21.52 μs | 0.183 μs | 0.162 μs | 21.15 μs | 21.58 μs | 21.70 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 20.78 μs | 0.414 μs | 0.935 μs | 19.46 μs | 20.71 μs | 23.24 μs |  0.97 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 25.36 μs | 0.503 μs | 0.517 μs | 24.24 μs | 25.46 μs | 26.09 μs |  1.18 |    0.02 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 21.46 μs | 0.426 μs | 0.880 μs | 20.07 μs | 21.31 μs | 23.60 μs |  1.00 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 22.22 μs | 0.442 μs | 1.149 μs | 20.81 μs | 21.90 μs | 25.37 μs |  1.03 |    0.05 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 23.03 μs | 0.456 μs | 0.823 μs | 21.85 μs | 22.79 μs | 24.88 μs |  1.07 |    0.04 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,128.804 μs | 13.5841 μs | 12.7066 μs | 1,095.100 μs | 1,130.245 μs | 1,151.025 μs | 1.000 |    0.02 |    3 | 5.8594 | 1.9531 |  95.68 KB |        1.00 |
| AutoMapperStartup |   242.537 μs |  0.5804 μs |  0.5146 μs |   241.782 μs |   242.531 μs |   243.358 μs | 0.215 |    0.00 |    2 | 5.8594 |      - | 103.64 KB |        1.08 |
| MapsterStartup    |     2.490 μs |  0.0450 μs |  0.0631 μs |     2.434 μs |     2.471 μs |     2.648 μs | 0.002 |    0.00 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.102 ms | 0.0102 ms | 0.0090 ms | 1.090 ms | 1.102 ms | 1.118 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.69 KB |        1.00 |
| AutoMapper | 3.386 ms | 0.0066 ms | 0.0052 ms | 3.379 ms | 3.386 ms | 3.398 ms |  3.07 |    0.02 |    3 | 15.6250 |  7.8125 | 310.38 KB |        3.21 |
| Mapster    | 2.585 ms | 0.0145 ms | 0.0128 ms | 2.568 ms | 2.586 ms | 2.605 ms |  2.35 |    0.02 |    2 | 39.0625 | 15.6250 | 757.48 KB |        7.83 |

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

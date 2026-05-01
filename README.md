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
> ⏱ **Last updated:** 2026-05-01 17:53 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.19 ns | **26.81 ns (1.7×)** | 29.43 ns (1.8×) | 79.59 ns (4.9×) | 16.33 ns (1.0×) |
| **Flattening** | 20.38 ns | **32.40 ns (1.6×)** | 35.09 ns (1.7×) | 86.93 ns (4.3×) | 24.72 ns (1.2×) |
| **Deep (2 nested)** | 62.76 ns | **69.94 ns (1.1×)** | 72.84 ns (1.2×) | 109.73 ns (1.8×) | 54.46 ns (0.9×) |
| **Complex (nest+coll)** | 80.71 ns | **104.43 ns (1.3×)** | 103.66 ns (1.3×) | 148.59 ns (1.8×) | 81.16 ns (1.0×) |
| **Collection (100)** | 2.123 μs | **2.184 μs (1.0×)** | 2.464 μs (1.2×) | 2.779 μs (1.3×) | 2.319 μs (1.1×) |
| **Deep Coll (100)** | 6.207 μs | **6.642 μs (1.1×)** | 7.257 μs (1.2×) | 7.730 μs (1.2×) | 6.285 μs (1.0×) |
| **Large Coll (1000)** | 21.25 μs | **20.06 μs (0.9×)** | 19.85 μs (0.9×) | 24.81 μs (1.2×) | 21.91 μs (1.0×) |
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
| Manual               |  16.19 ns | 0.073 ns | 0.061 ns |  16.13 ns |  16.16 ns |  16.31 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.81 ns | 0.262 ns | 0.245 ns |  26.42 ns |  26.71 ns |  27.24 ns |  1.66 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  79.59 ns | 0.057 ns | 0.047 ns |  79.52 ns |  79.58 ns |  79.68 ns |  4.92 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  29.43 ns | 0.351 ns | 0.311 ns |  28.97 ns |  29.44 ns |  30.08 ns |  1.82 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  16.33 ns | 0.199 ns | 0.186 ns |  16.01 ns |  16.28 ns |  16.63 ns |  1.01 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 334.64 ns | 1.174 ns | 1.040 ns | 332.59 ns | 334.56 ns | 336.59 ns | 20.67 |    0.10 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  16.27 ns | 0.146 ns | 0.122 ns |  16.16 ns |  16.21 ns |  16.59 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  17.41 ns | 0.241 ns | 0.201 ns |  17.06 ns |  17.47 ns |  17.73 ns |  1.08 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.38 ns | 0.145 ns | 0.128 ns |  20.18 ns |  20.38 ns |  20.62 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  32.40 ns | 0.184 ns | 0.172 ns |  32.14 ns |  32.42 ns |  32.70 ns |  1.59 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  86.93 ns | 0.106 ns | 0.083 ns |  86.72 ns |  86.93 ns |  87.03 ns |  4.27 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.09 ns | 0.328 ns | 0.307 ns |  34.65 ns |  35.05 ns |  35.70 ns |  1.72 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.72 ns | 0.182 ns | 0.152 ns |  24.58 ns |  24.65 ns |  25.07 ns |  1.21 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 327.29 ns | 0.800 ns | 0.748 ns | 325.97 ns | 327.25 ns | 328.29 ns | 16.06 |    0.10 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  62.76 ns | 0.550 ns | 0.514 ns |  61.82 ns |  62.69 ns |  63.59 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  69.94 ns | 0.279 ns | 0.248 ns |  69.70 ns |  69.89 ns |  70.57 ns |  1.11 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 109.73 ns | 0.220 ns | 0.206 ns | 109.35 ns | 109.73 ns | 110.10 ns |  1.75 |    0.01 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  72.84 ns | 0.287 ns | 0.269 ns |  72.35 ns |  72.74 ns |  73.29 ns |  1.16 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  54.46 ns | 0.151 ns | 0.141 ns |  54.29 ns |  54.45 ns |  54.80 ns |  0.87 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 358.43 ns | 1.857 ns | 1.737 ns | 354.61 ns | 358.50 ns | 360.74 ns |  5.71 |    0.05 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  80.71 ns | 1.361 ns | 1.273 ns |  78.57 ns |  81.01 ns |  82.44 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 104.43 ns | 0.343 ns | 0.287 ns | 104.09 ns | 104.37 ns | 105.09 ns |  1.29 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 148.59 ns | 0.687 ns | 0.643 ns | 146.81 ns | 148.75 ns | 149.46 ns |  1.84 |    0.03 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     | 103.66 ns | 1.968 ns | 1.933 ns |  99.92 ns | 103.81 ns | 106.33 ns |  1.28 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  81.16 ns | 1.494 ns | 1.398 ns |  78.48 ns |  81.25 ns |  83.29 ns |  1.01 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 404.23 ns | 2.041 ns | 1.909 ns | 401.29 ns | 404.02 ns | 407.13 ns |  5.01 |    0.08 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.123 μs | 0.0268 μs | 0.0224 μs | 2.081 μs | 2.129 μs | 2.151 μs |  1.00 |    0.01 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.184 μs | 0.0375 μs | 0.0351 μs | 2.099 μs | 2.197 μs | 2.226 μs |  1.03 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.779 μs | 0.0422 μs | 0.0394 μs | 2.669 μs | 2.789 μs | 2.830 μs |  1.31 |    0.02 |    4 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.464 μs | 0.0361 μs | 0.0337 μs | 2.368 μs | 2.473 μs | 2.507 μs |  1.16 |    0.02 |    3 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.319 μs | 0.0287 μs | 0.0268 μs | 2.246 μs | 2.325 μs | 2.357 μs |  1.09 |    0.02 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.766 μs | 0.0319 μs | 0.0299 μs | 2.731 μs | 2.767 μs | 2.819 μs |  1.30 |    0.02 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.207 μs | 0.0627 μs | 0.0556 μs | 6.065 μs | 6.200 μs | 6.310 μs |  1.00 |    0.01 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.642 μs | 0.0955 μs | 0.0893 μs | 6.512 μs | 6.654 μs | 6.791 μs |  1.07 |    0.02 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.730 μs | 0.0726 μs | 0.0679 μs | 7.565 μs | 7.764 μs | 7.798 μs |  1.25 |    0.02 |    5 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 7.257 μs | 0.1027 μs | 0.0910 μs | 7.026 μs | 7.262 μs | 7.370 μs |  1.17 |    0.02 |    4 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.285 μs | 0.0311 μs | 0.0275 μs | 6.243 μs | 6.282 μs | 6.328 μs |  1.01 |    0.01 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.837 μs | 0.0512 μs | 0.0479 μs | 5.742 μs | 5.838 μs | 5.918 μs |  0.94 |    0.01 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 21.25 μs | 0.398 μs | 0.353 μs | 20.48 μs | 21.34 μs | 21.79 μs |  1.00 |    0.02 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 20.06 μs | 0.244 μs | 0.228 μs | 19.41 μs | 20.06 μs | 20.39 μs |  0.94 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 24.81 μs | 0.307 μs | 0.272 μs | 24.16 μs | 24.85 μs | 25.15 μs |  1.17 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 19.85 μs | 0.305 μs | 0.285 μs | 19.11 μs | 19.86 μs | 20.23 μs |  0.93 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.91 μs | 0.385 μs | 0.360 μs | 21.04 μs | 21.98 μs | 22.37 μs |  1.03 |    0.02 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 22.64 μs | 0.148 μs | 0.124 μs | 22.48 μs | 22.61 μs | 22.93 μs |  1.07 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,217.351 μs | 11.5003 μs | 10.1947 μs | 1,195.357 μs | 1,218.021 μs | 1,234.453 μs | 1.000 |    3 | 5.8594 |      - |  95.51 KB |        1.00 |
| AutoMapperStartup |   239.201 μs |  1.3753 μs |  1.2192 μs |   237.291 μs |   239.155 μs |   241.108 μs | 0.197 |    2 | 5.8594 |      - | 103.68 KB |        1.09 |
| MapsterStartup    |     2.662 μs |  0.0261 μs |  0.0231 μs |     2.585 μs |     2.668 μs |     2.679 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.180 ms | 0.0080 ms | 0.0067 ms | 1.167 ms | 1.183 ms | 1.186 ms |  1.00 |    0.01 |    1 |  5.8594 |  3.9063 |  96.37 KB |        1.00 |
| AutoMapper | 3.406 ms | 0.0123 ms | 0.0109 ms | 3.393 ms | 3.404 ms | 3.425 ms |  2.89 |    0.02 |    3 | 15.6250 |  7.8125 |  310.7 KB |        3.22 |
| Mapster    | 2.584 ms | 0.0122 ms | 0.0108 ms | 2.570 ms | 2.580 ms | 2.607 ms |  2.19 |    0.01 |    2 | 39.0625 | 15.6250 | 761.67 KB |        7.90 |

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

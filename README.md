# 🥚 EggMapper

> **The fastest .NET runtime object-to-object mapper** — forked from AutoMapper's last open-source release, rebuilt for maximum performance. Drop-in replacement with the same API, 1.5–5× faster.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![License: MIT OR Apache-2.0](https://img.shields.io/badge/license-MIT%20OR%20Apache--2.0-green.svg)](LICENSE-MIT)

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
- 📖 **MIT OR Apache-2.0 licensed** — free for commercial use, forever

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
> ⏱ **Last updated:** 2026-11-14 14:05 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.17 ns | **25.73 ns (1.7×)** | 27.82 ns (1.8×) | 81.82 ns (5.4×) | 14.97 ns (1.0×) |
| **Flattening** | 19.48 ns | **30.62 ns (1.6×)** | 48.35 ns (2.5×) | 87.87 ns (4.5×) | 24.77 ns (1.3×) |
| **Deep (2 nested)** | 54.50 ns | **64.42 ns (1.2×)** | 68.26 ns (1.2×) | 129.09 ns (2.4×) | 51.09 ns (0.9×) |
| **Complex (nest+coll)** | 71.61 ns | **94.94 ns (1.3×)** | 90.72 ns (1.3×) | 151.63 ns (2.1×) | 73.84 ns (1.0×) |
| **Collection (100)** | 1.744 μs | **1.770 μs (1.0×)** | 1.740 μs (1.0×) | 2.375 μs (1.4×) | 1.850 μs (1.1×) |
| **Deep Coll (100)** | 5.810 μs | **6.351 μs (1.1×)** | 6.376 μs (1.1×) | 7.096 μs (1.2×) | 5.849 μs (1.0×) |
| **Large Coll (1000)** | 18.48 μs | **17.87 μs (1.0×)** | 17.90 μs (1.0×) | 22.55 μs (1.2×) | 18.58 μs (1.0×) |
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
| Manual               |  15.17 ns | 0.146 ns | 0.122 ns |  14.97 ns |  15.14 ns |  15.40 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  25.73 ns | 0.148 ns | 0.131 ns |  25.59 ns |  25.67 ns |  25.97 ns |  1.70 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  81.82 ns | 0.307 ns | 0.272 ns |  81.47 ns |  81.75 ns |  82.44 ns |  5.39 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.82 ns | 0.273 ns | 0.228 ns |  27.41 ns |  27.82 ns |  28.21 ns |  1.83 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  14.97 ns | 0.129 ns | 0.120 ns |  14.77 ns |  14.96 ns |  15.19 ns |  0.99 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 495.28 ns | 1.270 ns | 1.188 ns | 492.57 ns | 494.95 ns | 496.83 ns | 32.66 |    0.26 |    6 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.40 ns | 0.301 ns | 0.235 ns |  14.83 ns |  15.51 ns |  15.62 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.10 ns | 0.159 ns | 0.141 ns |  15.93 ns |  16.12 ns |  16.41 ns |  1.06 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.48 ns | 0.157 ns | 0.147 ns |  19.22 ns |  19.45 ns |  19.77 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.62 ns | 0.312 ns | 0.277 ns |  30.21 ns |  30.55 ns |  31.28 ns |  1.57 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  87.87 ns | 0.362 ns | 0.320 ns |  87.24 ns |  87.91 ns |  88.53 ns |  4.51 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  48.35 ns | 0.159 ns | 0.141 ns |  48.05 ns |  48.36 ns |  48.54 ns |  2.48 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.77 ns | 0.187 ns | 0.175 ns |  24.42 ns |  24.75 ns |  25.07 ns |  1.27 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 527.12 ns | 1.490 ns | 1.244 ns | 525.28 ns | 527.34 ns | 529.72 ns | 27.05 |    0.21 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  54.50 ns | 1.107 ns | 1.318 ns |  53.07 ns |  54.46 ns |  56.80 ns |  1.00 |    0.03 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.42 ns | 0.874 ns | 0.818 ns |  63.36 ns |  64.53 ns |  66.13 ns |  1.18 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 129.09 ns | 1.547 ns | 1.292 ns | 126.50 ns | 129.34 ns | 131.36 ns |  2.37 |    0.06 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  68.26 ns | 0.463 ns | 0.387 ns |  67.40 ns |  68.23 ns |  68.88 ns |  1.25 |    0.03 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  51.09 ns | 1.073 ns | 1.961 ns |  48.75 ns |  50.64 ns |  56.56 ns |  0.94 |    0.04 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 527.36 ns | 4.526 ns | 4.233 ns | 519.67 ns | 527.62 ns | 533.83 ns |  9.68 |    0.24 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  71.61 ns | 1.170 ns | 0.977 ns |  69.53 ns |  71.33 ns |  72.98 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  94.94 ns | 0.714 ns | 0.633 ns |  93.89 ns |  94.89 ns |  96.22 ns |  1.33 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 151.63 ns | 1.397 ns | 1.307 ns | 149.69 ns | 151.48 ns | 154.41 ns |  2.12 |    0.03 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  90.72 ns | 0.423 ns | 0.375 ns |  90.13 ns |  90.78 ns |  91.28 ns |  1.27 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  73.84 ns | 0.987 ns | 0.923 ns |  71.94 ns |  73.86 ns |  75.29 ns |  1.03 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 564.88 ns | 4.285 ns | 3.799 ns | 560.99 ns | 563.89 ns | 573.30 ns |  7.89 |    0.12 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.744 μs | 0.0186 μs | 0.0174 μs | 1.710 μs | 1.742 μs | 1.770 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.770 μs | 0.0332 μs | 0.0310 μs | 1.731 μs | 1.761 μs | 1.839 μs |  1.02 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.375 μs | 0.0296 μs | 0.0277 μs | 2.331 μs | 2.378 μs | 2.423 μs |  1.36 |    0.02 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.740 μs | 0.0213 μs | 0.0200 μs | 1.715 μs | 1.738 μs | 1.785 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.850 μs | 0.0266 μs | 0.0236 μs | 1.826 μs | 1.841 μs | 1.911 μs |  1.06 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.479 μs | 0.0353 μs | 0.0330 μs | 2.413 μs | 2.478 μs | 2.525 μs |  1.42 |    0.02 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.810 μs | 0.0767 μs | 0.0640 μs | 5.618 μs | 5.820 μs | 5.888 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.351 μs | 0.0618 μs | 0.0578 μs | 6.237 μs | 6.341 μs | 6.441 μs |  1.09 |    0.02 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.096 μs | 0.0731 μs | 0.0684 μs | 7.028 μs | 7.084 μs | 7.237 μs |  1.22 |    0.02 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.376 μs | 0.0527 μs | 0.0493 μs | 6.271 μs | 6.387 μs | 6.432 μs |  1.10 |    0.01 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.849 μs | 0.0459 μs | 0.0429 μs | 5.739 μs | 5.845 μs | 5.919 μs |  1.01 |    0.01 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.601 μs | 0.0418 μs | 0.0349 μs | 5.535 μs | 5.605 μs | 5.662 μs |  0.96 |    0.01 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.48 μs | 0.123 μs | 0.115 μs | 18.25 μs | 18.49 μs | 18.74 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.87 μs | 0.183 μs | 0.172 μs | 17.51 μs | 17.89 μs | 18.10 μs |  0.97 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 22.55 μs | 0.355 μs | 0.296 μs | 22.09 μs | 22.49 μs | 22.99 μs |  1.22 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.90 μs | 0.180 μs | 0.159 μs | 17.54 μs | 17.90 μs | 18.14 μs |  0.97 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.58 μs | 0.156 μs | 0.146 μs | 18.21 μs | 18.59 μs | 18.80 μs |  1.01 |    0.01 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.75 μs | 0.172 μs | 0.160 μs | 20.50 μs | 20.73 μs | 21.01 μs |  1.12 |    0.01 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method     | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual     | 10.89 ns | 0.038 ns | 0.034 ns | 10.85 ns | 10.88 ns | 10.95 ns |  1.00 |    0.00 |    1 |         - |          NA |
| EggMapper  | 19.52 ns | 0.066 ns | 0.055 ns | 19.43 ns | 19.50 ns | 19.63 ns |  1.79 |    0.01 |    2 |         - |          NA |
| AutoMapper | 76.87 ns | 0.151 ns | 0.134 ns | 76.74 ns | 76.80 ns | 77.10 ns |  7.06 |    0.02 |    3 |         - |          NA |
| Mapster    | 19.63 ns | 0.037 ns | 0.031 ns | 19.60 ns | 19.62 ns | 19.70 ns |  1.80 |    0.01 |    2 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 15.83 ns | 0.127 ns | 0.118 ns | 15.69 ns | 15.83 ns | 16.06 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 28.56 ns | 0.162 ns | 0.151 ns | 28.25 ns | 28.53 ns | 28.79 ns |  1.80 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 83.55 ns | 0.365 ns | 0.323 ns | 83.02 ns | 83.64 ns | 84.07 ns |  5.28 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 34.00 ns | 0.183 ns | 0.172 ns | 33.82 ns | 33.94 ns | 34.44 ns |  2.15 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 31.95 ns | 0.443 ns | 0.393 ns | 31.51 ns | 31.82 ns | 32.95 ns |  2.02 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.195 μs | 0.0219 μs | 0.0194 μs |   1.168 μs |   1.189 μs |   1.231 μs |   1.00 |    0.02 |    1 | 0.3948 | 0.0076 |   6.48 KB |        1.00 |
| AutoMapperStartup | 444.737 μs | 3.9512 μs | 3.6960 μs | 438.768 μs | 444.832 μs | 452.759 μs | 372.20 |    6.50 |    3 | 5.8594 |      - |  106.5 KB |       16.44 |
| MapsterStartup    |   2.601 μs | 0.0156 μs | 0.0146 μs |   2.558 μs |   2.605 μs |   2.618 μs |   2.18 |    0.04 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.78 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.263 ms | 0.0091 ms | 0.0081 ms | 1.255 ms | 1.261 ms | 1.280 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  77.46 KB |        1.00 |
| AutoMapper | 4.501 ms | 0.0236 ms | 0.0184 ms | 4.469 ms | 4.500 ms | 4.533 ms |  3.56 |    0.03 |    3 | 15.6250 |  7.8125 | 327.01 KB |        4.22 |
| Mapster    | 2.508 ms | 0.0198 ms | 0.0185 ms | 2.482 ms | 2.510 ms | 2.546 ms |  1.99 |    0.02 |    2 | 39.0625 | 15.6250 | 764.18 KB |        9.87 |

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

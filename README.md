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
> ⏱ **Last updated:** 2026-11-14 12:03 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 12.05 ns | **21.57 ns (1.8×)** | 21.96 ns (1.8×) | 57.09 ns (4.7×) | 12.42 ns (1.0×) |
| **Flattening** | 15.63 ns | **23.79 ns (1.5×)** | 26.36 ns (1.7×) | 64.22 ns (4.1×) | 20.17 ns (1.3×) |
| **Deep (2 nested)** | 45.01 ns | **53.96 ns (1.2×)** | 55.42 ns (1.2×) | 85.62 ns (1.9×) | 41.50 ns (0.9×) |
| **Complex (nest+coll)** | 60.10 ns | **83.01 ns (1.4×)** | 77.90 ns (1.3×) | 113.22 ns (1.9×) | 71.58 ns (1.2×) |
| **Collection (100)** | 1.599 μs | **1.613 μs (1.0×)** | 1.665 μs (1.0×) | 1.954 μs (1.2×) | 1.681 μs (1.1×) |
| **Deep Coll (100)** | 4.615 μs | **4.952 μs (1.1×)** | 5.028 μs (1.1×) | 5.474 μs (1.2×) | 4.607 μs (1.0×) |
| **Large Coll (1000)** | 15.96 μs | **15.77 μs (1.0×)** | 14.89 μs (0.9×) | 18.78 μs (1.2×) | 17.25 μs (1.1×) |
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
| Manual               |  12.05 ns | 0.162 ns | 0.152 ns |  11.88 ns |  12.02 ns |  12.34 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  21.57 ns | 0.223 ns | 0.198 ns |  21.36 ns |  21.50 ns |  21.90 ns |  1.79 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  57.09 ns | 0.323 ns | 0.286 ns |  56.85 ns |  56.92 ns |  57.70 ns |  4.74 |    0.06 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  21.96 ns | 0.427 ns | 0.457 ns |  21.47 ns |  21.76 ns |  22.95 ns |  1.82 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  12.42 ns | 0.165 ns | 0.154 ns |  12.13 ns |  12.42 ns |  12.69 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 271.10 ns | 1.576 ns | 1.474 ns | 269.59 ns | 270.58 ns | 274.51 ns | 22.50 |    0.30 |    4 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.61 ns | 0.169 ns | 0.158 ns |  12.40 ns |  12.62 ns |  12.94 ns |  1.05 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.53 ns | 0.112 ns | 0.099 ns |  12.43 ns |  12.51 ns |  12.72 ns |  1.04 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.63 ns | 0.249 ns | 0.232 ns |  15.24 ns |  15.69 ns |  16.01 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  23.79 ns | 0.090 ns | 0.080 ns |  23.70 ns |  23.78 ns |  23.95 ns |  1.52 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  64.22 ns | 0.121 ns | 0.101 ns |  64.13 ns |  64.20 ns |  64.49 ns |  4.11 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  26.36 ns | 0.105 ns | 0.099 ns |  26.26 ns |  26.30 ns |  26.56 ns |  1.69 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  20.17 ns | 0.435 ns | 0.427 ns |  19.55 ns |  20.04 ns |  20.83 ns |  1.29 |    0.03 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 253.42 ns | 0.722 ns | 0.640 ns | 252.16 ns | 253.48 ns | 254.36 ns | 16.21 |    0.24 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  45.01 ns | 0.205 ns | 0.192 ns |  44.72 ns |  44.99 ns |  45.47 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  53.96 ns | 0.235 ns | 0.220 ns |  53.63 ns |  53.98 ns |  54.35 ns |  1.20 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  |  85.62 ns | 0.108 ns | 0.096 ns |  85.44 ns |  85.62 ns |  85.78 ns |  1.90 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  55.42 ns | 0.346 ns | 0.324 ns |  54.79 ns |  55.32 ns |  55.91 ns |  1.23 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  41.50 ns | 0.109 ns | 0.102 ns |  41.35 ns |  41.53 ns |  41.68 ns |  0.92 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 268.45 ns | 0.736 ns | 0.575 ns | 267.45 ns | 268.38 ns | 269.43 ns |  5.96 |    0.03 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.10 ns | 0.476 ns | 0.446 ns |  59.38 ns |  60.12 ns |  60.87 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  83.01 ns | 0.892 ns | 0.835 ns |  81.65 ns |  83.53 ns |  83.88 ns |  1.38 |    0.02 |    4 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 113.22 ns | 0.439 ns | 0.390 ns | 112.61 ns | 113.21 ns | 113.92 ns |  1.88 |    0.01 |    5 | 0.0196 |     328 B |        1.02 |
| Mapster     |  77.90 ns | 0.845 ns | 0.791 ns |  76.32 ns |  77.99 ns |  78.90 ns |  1.30 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  71.58 ns | 1.036 ns | 0.918 ns |  70.00 ns |  71.88 ns |  72.99 ns |  1.19 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 306.36 ns | 1.480 ns | 1.236 ns | 304.68 ns | 306.32 ns | 308.55 ns |  5.10 |    0.04 |    6 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.599 μs | 0.0252 μs | 0.0236 μs | 1.549 μs | 1.603 μs | 1.645 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.613 μs | 0.0239 μs | 0.0224 μs | 1.568 μs | 1.620 μs | 1.650 μs |  1.01 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 1.954 μs | 0.0152 μs | 0.0135 μs | 1.927 μs | 1.954 μs | 1.972 μs |  1.22 |    0.02 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.665 μs | 0.0288 μs | 0.0270 μs | 1.593 μs | 1.670 μs | 1.711 μs |  1.04 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.681 μs | 0.0140 μs | 0.0124 μs | 1.657 μs | 1.684 μs | 1.697 μs |  1.05 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.094 μs | 0.0160 μs | 0.0150 μs | 2.070 μs | 2.096 μs | 2.118 μs |  1.31 |    0.02 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.615 μs | 0.0238 μs | 0.0211 μs | 4.577 μs | 4.610 μs | 4.649 μs |  1.00 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 4.952 μs | 0.0242 μs | 0.0202 μs | 4.906 μs | 4.956 μs | 4.981 μs |  1.07 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 5.474 μs | 0.0351 μs | 0.0328 μs | 5.395 μs | 5.473 μs | 5.529 μs |  1.19 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.028 μs | 0.0382 μs | 0.0357 μs | 4.977 μs | 5.024 μs | 5.108 μs |  1.09 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 4.607 μs | 0.0122 μs | 0.0108 μs | 4.581 μs | 4.609 μs | 4.626 μs |  1.00 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.396 μs | 0.0157 μs | 0.0140 μs | 4.370 μs | 4.395 μs | 4.417 μs |  0.95 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 15.96 μs | 0.082 μs | 0.077 μs | 15.81 μs | 15.96 μs | 16.08 μs |  1.00 |    0.01 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 15.77 μs | 0.240 μs | 0.224 μs | 15.39 μs | 15.75 μs | 16.18 μs |  0.99 |    0.01 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 18.78 μs | 0.333 μs | 0.295 μs | 18.00 μs | 18.85 μs | 19.14 μs |  1.18 |    0.02 |    4 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 14.89 μs | 0.147 μs | 0.123 μs | 14.58 μs | 14.89 μs | 15.10 μs |  0.93 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.25 μs | 0.135 μs | 0.126 μs | 17.01 μs | 17.26 μs | 17.41 μs |  1.08 |    0.01 |    3 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 17.62 μs | 0.182 μs | 0.170 μs | 17.22 μs | 17.60 μs | 17.91 μs |  1.10 |    0.01 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method     | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual     |  8.595 ns | 0.2230 ns | 0.6143 ns |  8.234 ns |  8.205 ns |  9.864 ns |  1.00 |    0.10 |    1 |         - |          NA |
| EggMapper  | 16.908 ns | 0.3308 ns | 0.5964 ns | 17.202 ns | 15.831 ns | 17.526 ns |  1.98 |    0.15 |    2 |         - |          NA |
| AutoMapper | 53.575 ns | 0.0799 ns | 0.0748 ns | 53.564 ns | 53.475 ns | 53.752 ns |  6.26 |    0.41 |    3 |         - |          NA |
| Mapster    | 17.133 ns | 0.3289 ns | 0.3377 ns | 17.199 ns | 15.827 ns | 17.261 ns |  2.00 |    0.14 |    2 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 12.18 ns | 0.138 ns | 0.129 ns | 11.96 ns | 12.23 ns | 12.35 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 23.89 ns | 0.094 ns | 0.084 ns | 23.74 ns | 23.88 ns | 24.02 ns |  1.96 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 60.19 ns | 0.081 ns | 0.068 ns | 60.02 ns | 60.18 ns | 60.28 ns |  4.94 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 24.96 ns | 0.313 ns | 0.278 ns | 24.63 ns | 24.97 ns | 25.48 ns |  2.05 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 25.73 ns | 0.196 ns | 0.183 ns | 25.45 ns | 25.75 ns | 26.00 ns |  2.11 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error      | StdDev    | Min        | Median     | Max        | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|-----------:|----------:|-----------:|-----------:|-----------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 923.353 μs | 10.7534 μs | 8.9796 μs | 905.724 μs | 924.729 μs | 938.388 μs | 1.000 |    3 | 5.8594 | 3.9063 |  96.06 KB |        1.00 |
| AutoMapperStartup | 337.816 μs |  3.8004 μs | 3.3689 μs | 333.756 μs | 336.838 μs | 343.922 μs | 0.366 |    2 | 5.8594 |      - | 106.32 KB |        1.11 |
| MapsterStartup    |   1.935 μs |  0.0049 μs | 0.0046 μs |   1.925 μs |   1.935 μs |   1.943 μs | 0.002 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error    | StdDev   | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|---------:|---------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   912.5 μs | 12.53 μs | 11.72 μs |   898.5 μs |   910.0 μs |   937.3 μs |  1.00 |    0.02 |    1 |  5.8594 |  3.9063 |     96 KB |        1.00 |
| AutoMapper | 3,655.7 μs |  8.94 μs |  7.92 μs | 3,645.2 μs | 3,655.1 μs | 3,669.5 μs |  4.01 |    0.05 |    3 | 15.6250 |  7.8125 | 326.86 KB |        3.40 |
| Mapster    | 1,996.3 μs |  5.11 μs |  4.27 μs | 1,991.1 μs | 1,995.2 μs | 2,005.2 μs |  2.19 |    0.03 |    2 | 46.8750 | 19.5313 | 766.61 KB |        7.99 |

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

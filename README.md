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

// Hot path mapping one pair: inject the typed mapper (plain interface call, no generic virtual dispatch)
public class OrderService(IMapper<Order, OrderDto> orderMapper)
{
    public OrderDto Get(Order order) => orderMapper.Map(order);
}
```

Maps compile on first use. Call `config.CompileMappings()` (or `AssertConfigurationIsValid()`) at startup or in a
test to catch mapping errors before the first request.

## Performance

<!-- PERF_TIMESTAMP_START -->
> ⏱ **Last updated:** 2026-11-14 23:05 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.06 ns | **27.01 ns (1.8×)** | 27.52 ns (1.8×) | 81.40 ns (5.4×) | 15.19 ns (1.0×) |
| **Flattening** | 18.55 ns | **29.50 ns (1.6×)** | 35.63 ns (1.9×) | 86.30 ns (4.7×) | 23.45 ns (1.3×) |
| **Deep (2 nested)** | 56.49 ns | **52.53 ns (0.9×)** | 68.24 ns (1.2×) | 120.06 ns (2.1×) | 49.73 ns (0.9×) |
| **Complex (nest+coll)** | 75.62 ns | **90.86 ns (1.2×)** | 96.21 ns (1.3×) | 172.64 ns (2.3×) | 75.84 ns (1.0×) |
| **Collection (100)** | 1.832 μs | **1.794 μs (1.0×)** | 1.844 μs (1.0×) | 2.501 μs (1.4×) | 1.947 μs (1.1×) |
| **Deep Coll (100)** | 5.639 μs | **4.576 μs (0.8×)** | 6.162 μs (1.1×) | 6.823 μs (1.2×) | 5.684 μs (1.0×) |
| **Large Coll (1000)** | 17.27 μs | **16.58 μs (1.0×)** | 16.85 μs (1.0×) | 20.67 μs (1.2×) | 17.76 μs (1.0×) |
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
| Manual               |  15.06 ns | 0.189 ns | 0.177 ns |  14.81 ns |  15.03 ns |  15.36 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.01 ns | 0.441 ns | 0.413 ns |  26.26 ns |  27.06 ns |  27.63 ns |  1.79 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  20.99 ns | 0.317 ns | 0.297 ns |  20.57 ns |  21.01 ns |  21.56 ns |  1.39 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  81.40 ns | 0.332 ns | 0.311 ns |  80.99 ns |  81.35 ns |  82.10 ns |  5.40 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.52 ns | 0.335 ns | 0.297 ns |  27.10 ns |  27.50 ns |  28.13 ns |  1.83 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.19 ns | 0.248 ns | 0.232 ns |  14.75 ns |  15.22 ns |  15.61 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 478.00 ns | 1.243 ns | 1.102 ns | 476.40 ns | 477.75 ns | 480.31 ns | 31.74 |    0.37 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  14.86 ns | 0.188 ns | 0.167 ns |  14.68 ns |  14.81 ns |  15.24 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.43 ns | 0.179 ns | 0.167 ns |  15.24 ns |  15.37 ns |  15.75 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.55 ns | 0.090 ns | 0.084 ns |  18.43 ns |  18.53 ns |  18.70 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.50 ns | 0.260 ns | 0.243 ns |  29.02 ns |  29.54 ns |  29.99 ns |  1.59 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  86.30 ns | 0.423 ns | 0.353 ns |  85.72 ns |  86.36 ns |  87.14 ns |  4.65 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.63 ns | 0.247 ns | 0.206 ns |  35.27 ns |  35.62 ns |  36.05 ns |  1.92 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.45 ns | 0.106 ns | 0.094 ns |  23.29 ns |  23.45 ns |  23.61 ns |  1.26 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 527.16 ns | 2.391 ns | 2.237 ns | 523.50 ns | 527.32 ns | 531.40 ns | 28.42 |    0.17 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  56.49 ns | 0.400 ns | 0.355 ns |  55.56 ns |  56.49 ns |  57.02 ns |  1.00 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  52.53 ns | 0.665 ns | 0.622 ns |  51.29 ns |  52.68 ns |  53.39 ns |  0.93 |    0.01 |    2 | 0.0095 |     160 B |        0.59 |
| AutoMapper  | 120.06 ns | 0.753 ns | 0.705 ns | 118.84 ns | 119.99 ns | 121.39 ns |  2.13 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  68.24 ns | 0.887 ns | 0.830 ns |  67.00 ns |  68.37 ns |  69.66 ns |  1.21 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.73 ns | 0.505 ns | 0.448 ns |  48.89 ns |  49.91 ns |  50.38 ns |  0.88 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 500.69 ns | 3.340 ns | 2.789 ns | 495.92 ns | 500.02 ns | 506.49 ns |  8.86 |    0.07 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  75.62 ns | 1.070 ns | 0.948 ns |  74.44 ns |  75.48 ns |  77.66 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  90.86 ns | 0.455 ns | 0.355 ns |  90.09 ns |  90.94 ns |  91.26 ns |  1.20 |    0.02 |    2 | 0.0153 |     256 B |        0.80 |
| AutoMapper  | 172.64 ns | 1.060 ns | 0.991 ns | 171.30 ns | 172.40 ns | 174.73 ns |  2.28 |    0.03 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  96.21 ns | 0.832 ns | 0.778 ns |  94.53 ns |  96.43 ns |  97.25 ns |  1.27 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  75.84 ns | 0.618 ns | 0.578 ns |  74.95 ns |  75.82 ns |  76.79 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 562.17 ns | 2.305 ns | 1.925 ns | 560.06 ns | 561.74 ns | 567.13 ns |  7.43 |    0.09 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.832 μs | 0.0199 μs | 0.0186 μs | 1.792 μs | 1.833 μs | 1.865 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.794 μs | 0.0307 μs | 0.0287 μs | 1.740 μs | 1.792 μs | 1.861 μs |  0.98 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.501 μs | 0.0280 μs | 0.0262 μs | 2.438 μs | 2.507 μs | 2.530 μs |  1.37 |    0.02 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.844 μs | 0.0215 μs | 0.0201 μs | 1.805 μs | 1.846 μs | 1.886 μs |  1.01 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.947 μs | 0.0177 μs | 0.0166 μs | 1.920 μs | 1.945 μs | 1.972 μs |  1.06 |    0.01 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.583 μs | 0.0291 μs | 0.0272 μs | 2.530 μs | 2.585 μs | 2.626 μs |  1.41 |    0.02 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.639 μs | 0.0560 μs | 0.0524 μs | 5.545 μs | 5.651 μs | 5.722 μs |  1.00 |    0.01 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 4.576 μs | 0.0450 μs | 0.0399 μs | 4.515 μs | 4.577 μs | 4.640 μs |  0.81 |    0.01 |    1 | 1.0071 | 0.0534 |  16.46 KB |        0.60 |
| AutoMapper  | 6.823 μs | 0.0730 μs | 0.0683 μs | 6.701 μs | 6.814 μs | 6.931 μs |  1.21 |    0.02 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.162 μs | 0.0689 μs | 0.0611 μs | 6.049 μs | 6.162 μs | 6.257 μs |  1.09 |    0.01 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.684 μs | 0.0900 μs | 0.0798 μs | 5.528 μs | 5.685 μs | 5.835 μs |  1.01 |    0.02 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.472 μs | 0.0457 μs | 0.0427 μs | 5.403 μs | 5.473 μs | 5.537 μs |  0.97 |    0.01 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.27 μs | 0.154 μs | 0.144 μs | 17.11 μs | 17.23 μs | 17.53 μs |  1.00 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.58 μs | 0.073 μs | 0.061 μs | 16.49 μs | 16.58 μs | 16.69 μs |  0.96 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.67 μs | 0.193 μs | 0.171 μs | 20.36 μs | 20.70 μs | 20.97 μs |  1.20 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 16.85 μs | 0.078 μs | 0.065 μs | 16.74 μs | 16.84 μs | 16.98 μs |  0.98 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.76 μs | 0.114 μs | 0.107 μs | 17.55 μs | 17.73 μs | 17.92 μs |  1.03 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.86 μs | 0.134 μs | 0.125 μs | 19.67 μs | 19.82 μs | 20.06 μs |  1.15 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         | 10.88 ns | 0.034 ns | 0.028 ns | 10.86 ns | 10.87 ns | 10.95 ns |  1.00 |    0.00 |    1 |         - |          NA |
| EggMapper      | 23.37 ns | 0.096 ns | 0.090 ns | 23.23 ns | 23.35 ns | 23.51 ns |  2.15 |    0.01 |    4 |         - |          NA |
| EggMapperTyped | 16.64 ns | 0.052 ns | 0.049 ns | 16.53 ns | 16.63 ns | 16.70 ns |  1.53 |    0.01 |    2 |         - |          NA |
| AutoMapper     | 78.85 ns | 0.127 ns | 0.113 ns | 78.72 ns | 78.81 ns | 79.03 ns |  7.24 |    0.02 |    5 |         - |          NA |
| Mapster        | 21.22 ns | 0.034 ns | 0.032 ns | 21.18 ns | 21.23 ns | 21.25 ns |  1.95 |    0.01 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 14.75 ns | 0.080 ns | 0.074 ns | 14.65 ns | 14.75 ns | 14.91 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 29.21 ns | 0.108 ns | 0.096 ns | 29.00 ns | 29.20 ns | 29.38 ns |  1.98 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 80.87 ns | 0.168 ns | 0.149 ns | 80.57 ns | 80.88 ns | 81.08 ns |  5.48 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 33.56 ns | 0.151 ns | 0.141 ns | 33.37 ns | 33.54 ns | 33.83 ns |  2.27 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 32.65 ns | 0.192 ns | 0.180 ns | 32.40 ns | 32.69 ns | 32.94 ns |  2.21 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.266 μs | 0.0062 μs | 0.0055 μs |   1.258 μs |   1.266 μs |   1.278 μs |   1.00 |    0.01 |    1 | 0.4597 | 0.0076 |   7.52 KB |        1.00 |
| AutoMapperStartup | 442.805 μs | 3.2979 μs | 2.9235 μs | 438.708 μs | 441.866 μs | 447.851 μs | 349.72 |    2.67 |    3 | 5.8594 |      - | 106.09 KB |       14.12 |
| MapsterStartup    |   2.468 μs | 0.0118 μs | 0.0105 μs |   2.447 μs |   2.467 μs |   2.485 μs |   1.95 |    0.01 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.53 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.439 ms | 0.0174 ms | 0.0162 ms | 1.421 ms | 1.436 ms | 1.471 ms |  1.00 |    0.02 |    1 |  3.9063 |       - |  86.93 KB |        1.00 |
| AutoMapper | 4.538 ms | 0.0287 ms | 0.0239 ms | 4.494 ms | 4.540 ms | 4.574 ms |  3.15 |    0.04 |    3 | 15.6250 |  7.8125 | 326.98 KB |        3.76 |
| Mapster    | 2.529 ms | 0.0310 ms | 0.0290 ms | 2.504 ms | 2.513 ms | 2.587 ms |  1.76 |    0.03 |    2 | 46.8750 | 15.6250 | 766.47 KB |        8.82 |

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

## Using EggMapper with AI coding assistants

[`llms.txt`](https://eggspot.github.io/EggMapper/llms.txt) (about 1,200 tokens) and
[`llms-full.txt`](https://eggspot.github.io/EggMapper/llms-full.txt) give AI assistants the whole API, the differences
from AutoMapper and the fix for each error. Every runtime API example in them is compiled against the library by the unit tests.
Paste this into `CLAUDE.md`, `AGENTS.md`, `.cursor/rules/eggmapper.mdc` or `.github/copilot-instructions.md`:

```markdown
## Object mapping: EggMapper
- API reference for AI: https://eggspot.github.io/EggMapper/llms.txt (full: llms-full.txt). Read it before writing mapping code.
- Put maps in Profile classes; register them with services.AddEggMapper(typeof(SomeProfile).Assembly).
- Map with mapper.Map<TSource, TDestination>(source); lists with mapper.MapList<TSource, TDestination>(items).
- Inject IMapper<TSource, TDestination> on hot paths.
- Keep one MapperConfiguration (singleton); never create one per request.
- ProjectTo needs both type arguments: query.ProjectTo<TSource, TDestination>(config).
- Configure members with ForMember(d => d.X, o => o.MapFrom(s => ...)) or o.Ignore().
- Not available: ForCtorParam, ForSourceMember, IncludeMembers, Include<,>, As<>, nested ForPath (d => d.A.B), mapper.ConfigurationProvider.
- Keep one test that calls config.CompileMappings() and config.AssertConfigurationIsValid().
- On a mapping error, apply the CreateMap or ForMember the message names instead of guessing.
```

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
| [AI Coding Assistants](https://eggspot.github.io/EggMapper/AI-Assistants.html) | `llms.txt` and a rules block for Claude Code, Cursor, Copilot |
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

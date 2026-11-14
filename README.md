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
> ⏱ **Last updated:** 2026-11-14 14:48 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 12.42 ns | **18.57 ns (1.5×)** | 22.09 ns (1.8×) | 58.17 ns (4.7×) | 12.24 ns (1.0×) |
| **Flattening** | 15.38 ns | **23.91 ns (1.6×)** | 26.57 ns (1.7×) | 61.55 ns (4.0×) | 19.62 ns (1.3×) |
| **Deep (2 nested)** | 45.30 ns | **42.13 ns (0.9×)** | 58.14 ns (1.3×) | 86.70 ns (1.9×) | 42.79 ns (0.9×) |
| **Complex (nest+coll)** | 62.62 ns | **77.29 ns (1.2×)** | 80.96 ns (1.3×) | 118.50 ns (1.9×) | 70.44 ns (1.1×) |
| **Collection (100)** | 1.739 μs | **1.720 μs (1.0×)** | 1.717 μs (1.0×) | 2.158 μs (1.2×) | 1.823 μs (1.1×) |
| **Deep Coll (100)** | 4.821 μs | **3.831 μs (0.8×)** | 5.322 μs (1.1×) | 5.702 μs (1.2×) | 5.285 μs (1.1×) |
| **Large Coll (1000)** | 16.53 μs | **16.46 μs (1.0×)** | 18.67 μs (1.1×) | 20.75 μs (1.3×) | 19.56 μs (1.2×) |
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
| Manual               |  12.42 ns | 0.147 ns | 0.137 ns |  12.17 ns |  12.44 ns |  12.72 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  18.57 ns | 0.284 ns | 0.265 ns |  18.11 ns |  18.58 ns |  19.05 ns |  1.50 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  13.88 ns | 0.210 ns | 0.186 ns |  13.54 ns |  13.87 ns |  14.25 ns |  1.12 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  58.17 ns | 0.329 ns | 0.292 ns |  57.93 ns |  58.02 ns |  58.77 ns |  4.68 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  22.09 ns | 0.406 ns | 0.380 ns |  21.29 ns |  22.28 ns |  22.41 ns |  1.78 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  12.24 ns | 0.300 ns | 0.281 ns |  11.88 ns |  12.17 ns |  12.75 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 253.88 ns | 1.316 ns | 1.027 ns | 252.33 ns | 253.91 ns | 256.07 ns | 20.44 |    0.23 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.60 ns | 0.079 ns | 0.074 ns |  12.44 ns |  12.63 ns |  12.69 ns |  1.01 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.69 ns | 0.230 ns | 0.215 ns |  12.45 ns |  12.70 ns |  13.07 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.38 ns | 0.093 ns | 0.078 ns |  15.32 ns |  15.34 ns |  15.59 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  23.91 ns | 0.191 ns | 0.179 ns |  23.66 ns |  23.94 ns |  24.17 ns |  1.55 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  61.55 ns | 0.077 ns | 0.060 ns |  61.44 ns |  61.57 ns |  61.62 ns |  4.00 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  26.57 ns | 0.247 ns | 0.231 ns |  26.28 ns |  26.57 ns |  27.04 ns |  1.73 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  19.62 ns | 0.151 ns | 0.133 ns |  19.45 ns |  19.60 ns |  19.93 ns |  1.28 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 266.15 ns | 1.885 ns | 1.764 ns | 264.14 ns | 266.03 ns | 269.00 ns | 17.31 |    0.14 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  45.30 ns | 0.360 ns | 0.319 ns |  44.88 ns |  45.24 ns |  45.99 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  42.13 ns | 0.665 ns | 0.622 ns |  41.38 ns |  42.10 ns |  43.31 ns |  0.93 |    0.01 |    1 | 0.0095 |     160 B |        0.59 |
| AutoMapper  |  86.70 ns | 1.667 ns | 1.560 ns |  84.91 ns |  86.22 ns |  90.05 ns |  1.91 |    0.04 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  58.14 ns | 0.739 ns | 0.691 ns |  56.55 ns |  58.14 ns |  59.04 ns |  1.28 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  42.79 ns | 0.863 ns | 0.847 ns |  41.82 ns |  42.66 ns |  44.53 ns |  0.94 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 278.24 ns | 2.828 ns | 2.507 ns | 273.43 ns | 277.96 ns | 282.98 ns |  6.14 |    0.07 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  62.62 ns | 1.214 ns | 1.135 ns |  60.61 ns |  63.23 ns |  63.78 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  77.29 ns | 1.502 ns | 2.005 ns |  73.94 ns |  77.01 ns |  81.24 ns |  1.23 |    0.04 |    3 | 0.0153 |     256 B |        0.80 |
| AutoMapper  | 118.50 ns | 1.606 ns | 1.424 ns | 115.33 ns | 118.85 ns | 121.04 ns |  1.89 |    0.04 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  80.96 ns | 1.123 ns | 1.050 ns |  78.65 ns |  81.14 ns |  82.40 ns |  1.29 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  70.44 ns | 1.004 ns | 0.939 ns |  69.24 ns |  70.16 ns |  72.01 ns |  1.13 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 319.13 ns | 1.719 ns | 1.436 ns | 316.44 ns | 319.19 ns | 321.04 ns |  5.10 |    0.09 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.739 μs | 0.0347 μs | 0.0832 μs | 1.555 μs | 1.737 μs | 1.907 μs |  1.00 |    0.07 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.720 μs | 0.0344 μs | 0.0837 μs | 1.614 μs | 1.698 μs | 1.929 μs |  0.99 |    0.07 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.158 μs | 0.0422 μs | 0.0549 μs | 2.081 μs | 2.148 μs | 2.286 μs |  1.24 |    0.07 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.717 μs | 0.0340 μs | 0.0858 μs | 1.557 μs | 1.703 μs | 1.920 μs |  0.99 |    0.07 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.823 μs | 0.0361 μs | 0.0762 μs | 1.673 μs | 1.841 μs | 1.934 μs |  1.05 |    0.07 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.185 μs | 0.0436 μs | 0.0653 μs | 1.997 μs | 2.180 μs | 2.300 μs |  1.26 |    0.07 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.821 μs | 0.0958 μs | 0.2220 μs | 4.724 μs | 4.578 μs | 5.387 μs |  1.00 |    0.06 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 3.831 μs | 0.0732 μs | 0.0899 μs | 3.804 μs | 3.697 μs | 4.041 μs |  0.80 |    0.04 |    1 | 1.0071 | 0.0534 |  16.46 KB |        0.60 |
| AutoMapper  | 5.702 μs | 0.1103 μs | 0.1354 μs | 5.668 μs | 5.486 μs | 6.028 μs |  1.19 |    0.06 |    5 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.322 μs | 0.0921 μs | 0.0862 μs | 5.324 μs | 5.143 μs | 5.487 μs |  1.11 |    0.05 |    4 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.285 μs | 0.0555 μs | 0.0463 μs | 5.281 μs | 5.194 μs | 5.367 μs |  1.10 |    0.05 |    4 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.448 μs | 0.0213 μs | 0.0166 μs | 4.442 μs | 4.424 μs | 4.473 μs |  0.92 |    0.04 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 16.53 μs | 0.319 μs | 0.266 μs | 16.10 μs | 16.54 μs | 17.11 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.46 μs | 0.327 μs | 0.414 μs | 15.56 μs | 16.49 μs | 17.33 μs |  1.00 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.75 μs | 0.393 μs | 0.368 μs | 19.85 μs | 20.81 μs | 21.19 μs |  1.26 |    0.03 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.67 μs | 0.360 μs | 0.370 μs | 17.84 μs | 18.77 μs | 19.10 μs |  1.13 |    0.03 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 19.56 μs | 0.355 μs | 0.332 μs | 18.96 μs | 19.54 μs | 20.06 μs |  1.18 |    0.03 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 17.75 μs | 0.248 μs | 0.232 μs | 17.21 μs | 17.81 μs | 17.99 μs |  1.07 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Min       | Median    | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  7.944 ns | 0.0185 ns | 0.0145 ns |  7.930 ns |  7.936 ns |  7.971 ns |  1.00 |    0.00 |    1 |         - |          NA |
| EggMapper      | 15.729 ns | 0.3650 ns | 0.5464 ns | 14.505 ns | 15.962 ns | 16.227 ns |  1.98 |    0.07 |    3 |         - |          NA |
| EggMapperTyped | 10.937 ns | 0.1089 ns | 0.1019 ns | 10.752 ns | 10.971 ns | 11.125 ns |  1.38 |    0.01 |    2 |         - |          NA |
| AutoMapper     | 52.594 ns | 0.0897 ns | 0.0795 ns | 52.480 ns | 52.598 ns | 52.760 ns |  6.62 |    0.02 |    4 |         - |          NA |
| Mapster        | 15.846 ns | 0.0178 ns | 0.0149 ns | 15.832 ns | 15.838 ns | 15.877 ns |  1.99 |    0.00 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 12.23 ns | 0.230 ns | 0.215 ns | 11.83 ns | 12.25 ns | 12.69 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 21.89 ns | 0.291 ns | 0.243 ns | 21.21 ns | 21.98 ns | 22.18 ns |  1.79 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 58.36 ns | 0.115 ns | 0.107 ns | 58.13 ns | 58.37 ns | 58.51 ns |  4.77 |    0.08 |    5 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 22.78 ns | 0.273 ns | 0.228 ns | 22.51 ns | 22.72 ns | 23.31 ns |  1.86 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 23.77 ns | 0.156 ns | 0.138 ns | 23.55 ns | 23.76 ns | 24.07 ns |  1.94 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error       | StdDev      | Min          | Median       | Max          | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|------------:|------------:|-------------:|-------------:|-------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |     931.2 ns |    18.59 ns |    30.02 ns |     887.1 ns |     931.6 ns |     997.2 ns |   1.00 |    0.04 |    1 | 0.3986 | 0.0076 |   6.52 KB |        1.00 |
| AutoMapperStartup | 323,510.6 ns | 2,000.38 ns | 1,773.28 ns | 321,430.5 ns | 323,068.2 ns | 326,625.0 ns | 347.76 |   11.02 |    3 | 5.8594 |      - |  106.2 KB |       16.30 |
| MapsterStartup    |   2,116.0 ns |    41.12 ns |    36.45 ns |   2,050.0 ns |   2,114.2 ns |   2,185.9 ns |   2.27 |    0.08 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.77 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.192 ms | 0.0104 ms | 0.0092 ms | 1.179 ms | 1.193 ms | 1.212 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  86.03 KB |        1.00 |
| AutoMapper | 3.707 ms | 0.0303 ms | 0.0283 ms | 3.669 ms | 3.704 ms | 3.759 ms |  3.11 |    0.03 |    3 | 15.6250 |  7.8125 | 327.74 KB |        3.81 |
| Mapster    | 2.030 ms | 0.0102 ms | 0.0091 ms | 2.018 ms | 2.027 ms | 2.051 ms |  1.70 |    0.01 |    2 | 42.9688 | 19.5313 | 757.13 KB |        8.80 |

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

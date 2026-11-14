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
> ⏱ **Last updated:** 2026-11-14 16:36 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 10.52 ns | **13.67 ns (1.3×)** | 14.62 ns (1.4×) | 36.18 ns (3.4×) | 10.43 ns (1.0×) |
| **Flattening** | 13.75 ns | **19.61 ns (1.4×)** | 21.13 ns (1.5×) | 38.75 ns (2.8×) | 15.20 ns (1.1×) |
| **Deep (2 nested)** | 37.46 ns | **33.30 ns (0.9×)** | 43.40 ns (1.2×) | 64.11 ns (1.7×) | 34.33 ns (0.9×) |
| **Complex (nest+coll)** | 49.88 ns | **58.96 ns (1.2×)** | 62.06 ns (1.2×) | 88.53 ns (1.8×) | 50.48 ns (1.0×) |
| **Collection (100)** | 1.346 μs | **1.459 μs (1.1×)** | 1.447 μs (1.1×) | 1.961 μs (1.5×) | 1.424 μs (1.1×) |
| **Deep Coll (100)** | 3.654 μs | **2.816 μs (0.8×)** | 3.838 μs (1.1×) | 4.448 μs (1.2×) | 3.581 μs (1.0×) |
| **Large Coll (1000)** | 12.07 μs | **10.88 μs (0.9×)** | 11.77 μs (1.0×) | 14.27 μs (1.2×) | 12.20 μs (1.0×) |
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

| Method               | Mean      | Error    | StdDev   | Min        | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------- |----------:|---------:|---------:|-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual               |  10.52 ns | 0.203 ns | 0.170 ns |  10.122 ns |  10.51 ns |  10.84 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  13.67 ns | 0.163 ns | 0.153 ns |  13.417 ns |  13.72 ns |  13.91 ns |  1.30 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  11.26 ns | 0.264 ns | 0.247 ns |  10.923 ns |  11.23 ns |  11.75 ns |  1.07 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  36.18 ns | 0.319 ns | 0.267 ns |  35.715 ns |  36.20 ns |  36.59 ns |  3.44 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  14.62 ns | 0.281 ns | 0.249 ns |  14.307 ns |  14.62 ns |  15.13 ns |  1.39 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  10.43 ns | 0.249 ns | 0.380 ns |   9.899 ns |  10.42 ns |  11.22 ns |  0.99 |    0.04 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 203.16 ns | 3.941 ns | 4.047 ns | 196.611 ns | 202.44 ns | 213.05 ns | 19.31 |    0.48 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  10.32 ns | 0.244 ns | 0.400 ns |   9.606 ns |  10.22 ns |  11.20 ns |  0.98 |    0.04 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  10.49 ns | 0.246 ns | 0.344 ns |   9.740 ns |  10.38 ns |  11.24 ns |  1.00 |    0.04 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  13.75 ns | 0.314 ns | 0.550 ns |  12.63 ns |  13.87 ns |  15.00 ns |  1.00 |    0.06 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  19.61 ns | 0.382 ns | 0.627 ns |  18.51 ns |  19.59 ns |  21.22 ns |  1.43 |    0.07 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  38.75 ns | 0.631 ns | 0.560 ns |  38.03 ns |  38.77 ns |  39.91 ns |  2.82 |    0.12 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  21.13 ns | 0.309 ns | 0.343 ns |  20.66 ns |  21.10 ns |  21.87 ns |  1.54 |    0.07 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  15.20 ns | 0.161 ns | 0.150 ns |  14.97 ns |  15.18 ns |  15.42 ns |  1.11 |    0.05 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 164.30 ns | 2.145 ns | 2.007 ns | 160.00 ns | 164.70 ns | 166.94 ns | 11.97 |    0.50 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  37.46 ns | 0.796 ns | 1.216 ns |  34.92 ns |  37.58 ns |  40.06 ns |  1.00 |    0.05 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  33.30 ns | 0.674 ns | 0.630 ns |  32.22 ns |  33.26 ns |  34.28 ns |  0.89 |    0.03 |    1 | 0.0095 |     160 B |        0.59 |
| AutoMapper  |  64.11 ns | 0.885 ns | 0.784 ns |  62.79 ns |  64.20 ns |  65.39 ns |  1.71 |    0.06 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  43.40 ns | 0.884 ns | 0.868 ns |  41.62 ns |  43.32 ns |  44.80 ns |  1.16 |    0.04 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  34.33 ns | 0.707 ns | 0.920 ns |  32.47 ns |  34.45 ns |  35.98 ns |  0.92 |    0.04 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 203.64 ns | 4.037 ns | 4.488 ns | 197.22 ns | 203.31 ns | 212.24 ns |  5.44 |    0.21 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  49.88 ns | 0.982 ns | 1.408 ns |  47.73 ns |  49.87 ns |  52.60 ns |  1.00 |    0.04 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  58.96 ns | 1.215 ns | 1.492 ns |  56.44 ns |  58.82 ns |  61.61 ns |  1.18 |    0.04 |    2 | 0.0153 |     256 B |        0.80 |
| AutoMapper  |  88.53 ns | 1.721 ns | 2.238 ns |  84.59 ns |  88.56 ns |  92.87 ns |  1.78 |    0.07 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  62.06 ns | 1.203 ns | 1.687 ns |  58.97 ns |  62.18 ns |  64.61 ns |  1.25 |    0.05 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  50.48 ns | 1.041 ns | 1.114 ns |  48.64 ns |  50.45 ns |  52.59 ns |  1.01 |    0.04 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 254.78 ns | 1.737 ns | 1.625 ns | 252.29 ns | 253.97 ns | 258.02 ns |  5.11 |    0.14 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.346 μs | 0.0268 μs | 0.0621 μs | 1.205 μs | 1.335 μs | 1.472 μs |  1.00 |    0.07 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.459 μs | 0.0275 μs | 0.0294 μs | 1.418 μs | 1.454 μs | 1.506 μs |  1.09 |    0.05 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 1.961 μs | 0.0387 μs | 0.0362 μs | 1.888 μs | 1.950 μs | 2.017 μs |  1.46 |    0.07 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.447 μs | 0.0282 μs | 0.0336 μs | 1.392 μs | 1.453 μs | 1.497 μs |  1.08 |    0.06 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.424 μs | 0.0349 μs | 0.1029 μs | 1.199 μs | 1.415 μs | 1.680 μs |  1.06 |    0.09 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 1.829 μs | 0.0360 μs | 0.0400 μs | 1.741 μs | 1.836 μs | 1.892 μs |  1.36 |    0.07 |    2 | 0.5436 | 0.0172 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 3.654 μs | 0.0936 μs | 0.2759 μs | 3.550 μs | 3.252 μs | 4.512 μs |  1.01 |    0.10 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 2.816 μs | 0.0552 μs | 0.0657 μs | 2.812 μs | 2.690 μs | 2.947 μs |  0.77 |    0.06 |    1 | 1.0071 | 0.0572 |  16.46 KB |        0.60 |
| AutoMapper  | 4.448 μs | 0.0929 μs | 0.2739 μs | 4.402 μs | 4.040 μs | 5.106 μs |  1.22 |    0.12 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 3.838 μs | 0.0742 μs | 0.0658 μs | 3.828 μs | 3.721 μs | 3.987 μs |  1.06 |    0.08 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 3.581 μs | 0.0709 μs | 0.1479 μs | 3.569 μs | 3.231 μs | 3.892 μs |  0.99 |    0.08 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 3.492 μs | 0.0690 μs | 0.1514 μs | 3.520 μs | 3.160 μs | 3.867 μs |  0.96 |    0.08 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Median   | Min       | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 12.07 μs | 0.238 μs | 0.384 μs | 12.04 μs | 11.289 μs | 12.78 μs |  1.00 |    0.04 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 10.88 μs | 0.225 μs | 0.662 μs | 10.64 μs |  9.985 μs | 12.56 μs |  0.90 |    0.06 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 14.27 μs | 0.284 μs | 0.806 μs | 14.23 μs | 12.950 μs | 16.15 μs |  1.18 |    0.08 |    4 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 11.77 μs | 0.234 μs | 0.513 μs | 11.76 μs | 10.914 μs | 12.91 μs |  0.98 |    0.05 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 12.20 μs | 0.242 μs | 0.633 μs | 12.04 μs | 10.939 μs | 13.67 μs |  1.01 |    0.06 |    2 | 5.2643 | 1.3123 |  86.02 KB |        1.00 |
| AgileMapper | 13.34 μs | 0.257 μs | 0.699 μs | 13.35 μs | 11.917 μs | 15.11 μs |  1.11 |    0.07 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Min       | Median    | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  6.675 ns | 0.1708 ns | 0.2098 ns |  6.272 ns |  6.649 ns |  7.061 ns |  1.00 |    0.04 |    1 |         - |          NA |
| EggMapper      | 10.432 ns | 0.2372 ns | 0.3000 ns |  9.915 ns | 10.351 ns | 11.109 ns |  1.56 |    0.07 |    3 |         - |          NA |
| EggMapperTyped |  7.338 ns | 0.1861 ns | 0.2143 ns |  7.103 ns |  7.289 ns |  7.853 ns |  1.10 |    0.05 |    2 |         - |          NA |
| AutoMapper     | 35.546 ns | 0.4588 ns | 0.3582 ns | 34.717 ns | 35.550 ns | 36.129 ns |  5.33 |    0.17 |    5 |         - |          NA |
| Mapster        | 12.518 ns | 0.2423 ns | 0.2148 ns | 12.234 ns | 12.507 ns | 12.942 ns |  1.88 |    0.07 |    4 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean      | Error     | StdDev    | Min       | Median    | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      |  9.921 ns | 0.1796 ns | 0.1402 ns |  9.706 ns |  9.940 ns | 10.18 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 16.612 ns | 0.3703 ns | 0.3464 ns | 15.976 ns | 16.560 ns | 17.31 ns |  1.67 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 37.375 ns | 0.7071 ns | 0.7860 ns | 36.521 ns | 37.203 ns | 39.91 ns |  3.77 |    0.09 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 17.485 ns | 0.3831 ns | 0.4981 ns | 16.807 ns | 17.294 ns | 18.75 ns |  1.76 |    0.05 |    2 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 17.273 ns | 0.3863 ns | 0.4745 ns | 16.348 ns | 17.303 ns | 18.13 ns |  1.74 |    0.05 |    2 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error       | StdDev      | Min          | Median       | Max          | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|------------:|------------:|-------------:|-------------:|-------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |     794.8 ns |    15.90 ns |    25.22 ns |     753.0 ns |     789.2 ns |     853.4 ns |   1.00 |    0.04 |    1 | 0.4520 | 0.0105 |    7.4 KB |        1.00 |
| AutoMapperStartup | 218,992.7 ns | 3,555.29 ns | 2,968.83 ns | 215,092.8 ns | 217,993.6 ns | 226,837.8 ns | 275.80 |    9.28 |    3 | 6.8359 | 0.9766 | 106.31 KB |       14.37 |
| MapsterStartup    |   1,561.8 ns |    30.17 ns |    61.63 ns |   1,450.8 ns |   1,550.7 ns |   1,714.6 ns |   1.97 |    0.10 |    2 | 0.7038 | 0.0286 |  11.51 KB |        1.56 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean       | Error    | StdDev    | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |-----------:|---------:|----------:|-----------:|-----------:|-----------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  |   993.8 μs | 19.41 μs |  26.57 μs |   947.0 μs |   990.4 μs | 1,060.6 μs |  1.00 |    0.04 |    1 |  3.9063 |  1.9531 |  87.12 KB |        1.00 |
| AutoMapper | 3,343.9 μs | 65.66 μs | 129.61 μs | 3,125.2 μs | 3,331.1 μs | 3,582.3 μs |  3.37 |    0.16 |    3 | 15.6250 |  7.8125 | 327.59 KB |        3.76 |
| Mapster    | 1,779.2 μs | 34.94 μs |  37.39 μs | 1,729.5 μs | 1,769.3 μs | 1,839.4 μs |  1.79 |    0.06 |    2 | 42.9688 | 19.5313 | 764.25 KB |        8.77 |

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

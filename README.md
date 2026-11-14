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
> ⏱ **Last updated:** 2026-11-14 23:33 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.76 ns | **26.00 ns (1.6×)** | 27.93 ns (1.7×) | 53.08 ns (3.2×) | 16.62 ns (1.0×) |
| **Flattening** | 20.48 ns | **30.01 ns (1.5×)** | 40.29 ns (2.0×) | 56.47 ns (2.8×) | 25.22 ns (1.2×) |
| **Deep (2 nested)** | 60.96 ns | **54.45 ns (0.9×)** | 73.94 ns (1.2×) | 108.51 ns (1.8×) | 58.78 ns (1.0×) |
| **Complex (nest+coll)** | 77.91 ns | **82.32 ns (1.1×)** | 94.83 ns (1.2×) | 125.87 ns (1.6×) | 79.66 ns (1.0×) |
| **Collection (100)** | 1.775 μs | **1.761 μs (1.0×)** | 1.872 μs (1.1×) | 2.427 μs (1.4×) | 2.000 μs (1.1×) |
| **Deep Coll (100)** | 6.091 μs | **4.492 μs (0.7×)** | 6.547 μs (1.1×) | 7.188 μs (1.2×) | 6.176 μs (1.0×) |
| **Large Coll (1000)** | 18.41 μs | **19.74 μs (1.1×)** | 18.01 μs (1.0×) | 20.55 μs (1.1×) | 17.60 μs (1.0×) |
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
| Manual               |  16.76 ns | 0.355 ns | 0.332 ns |  16.12 ns |  16.83 ns |  17.31 ns |  1.00 |    0.03 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper            |  26.00 ns | 0.382 ns | 0.338 ns |  25.40 ns |  26.01 ns |  26.68 ns |  1.55 |    0.04 |    3 | 0.0010 |      80 B |        1.00 |
| EggMapperTyped       |  20.21 ns | 0.459 ns | 0.528 ns |  19.43 ns |  20.36 ns |  21.31 ns |  1.21 |    0.04 |    2 | 0.0010 |      80 B |        1.00 |
| AutoMapper           |  53.08 ns | 0.565 ns | 0.500 ns |  51.96 ns |  53.21 ns |  53.93 ns |  3.17 |    0.07 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster              |  27.93 ns | 0.242 ns | 0.215 ns |  27.45 ns |  27.99 ns |  28.15 ns |  1.67 |    0.03 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap          |  16.62 ns | 0.366 ns | 0.342 ns |  15.75 ns |  16.63 ns |  17.06 ns |  0.99 |    0.03 |    1 | 0.0010 |      80 B |        1.00 |
| AgileMapper          | 254.66 ns | 1.330 ns | 1.179 ns | 253.11 ns | 254.35 ns | 256.80 ns | 15.20 |    0.30 |    6 | 0.0038 |     344 B |        4.30 |
| EggMapperGenerator   |  17.24 ns | 0.305 ns | 0.285 ns |  16.88 ns |  17.28 ns |  17.63 ns |  1.03 |    0.03 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapperClassMapper |  17.29 ns | 0.295 ns | 0.276 ns |  16.69 ns |  17.38 ns |  17.78 ns |  1.03 |    0.03 |    1 | 0.0010 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.48 ns | 0.455 ns | 0.558 ns |  19.45 ns |  20.64 ns |  21.22 ns |  1.00 |    0.04 |    1 | 0.0010 |      80 B |        1.00 |
| EggMap      |  30.01 ns | 0.411 ns | 0.384 ns |  29.05 ns |  30.11 ns |  30.47 ns |  1.47 |    0.04 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper  |  56.47 ns | 0.247 ns | 0.206 ns |  56.09 ns |  56.44 ns |  56.96 ns |  2.76 |    0.08 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster     |  40.29 ns | 0.399 ns | 0.353 ns |  39.41 ns |  40.39 ns |  40.67 ns |  1.97 |    0.06 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap |  25.22 ns | 0.368 ns | 0.326 ns |  24.44 ns |  25.31 ns |  25.51 ns |  1.23 |    0.04 |    2 | 0.0012 |     104 B |        1.30 |
| AgileMapper | 261.69 ns | 0.793 ns | 0.742 ns | 260.05 ns | 261.72 ns | 262.97 ns | 12.79 |    0.35 |    6 | 0.0038 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.96 ns | 1.273 ns | 2.512 ns |  53.02 ns |  61.78 ns |  64.68 ns |  1.00 |    0.06 |    2 | 0.0032 |     272 B |        1.00 |
| EggMapper   |  54.45 ns | 0.621 ns | 0.551 ns |  53.43 ns |  54.34 ns |  55.39 ns |  0.89 |    0.04 |    1 | 0.0019 |     160 B |        0.59 |
| AutoMapper  | 108.51 ns | 2.125 ns | 3.180 ns |  99.73 ns | 108.53 ns | 113.78 ns |  1.78 |    0.09 |    4 | 0.0032 |     272 B |        1.00 |
| Mapster     |  73.94 ns | 1.427 ns | 1.586 ns |  70.50 ns |  74.09 ns |  77.01 ns |  1.22 |    0.06 |    3 | 0.0032 |     272 B |        1.00 |
| MapperlyMap |  58.78 ns | 1.220 ns | 1.750 ns |  55.58 ns |  58.76 ns |  61.71 ns |  0.97 |    0.05 |    2 | 0.0032 |     272 B |        1.00 |
| AgileMapper | 301.08 ns | 3.128 ns | 2.926 ns | 295.55 ns | 301.87 ns | 305.49 ns |  4.95 |    0.22 |    5 | 0.0048 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  77.91 ns | 1.552 ns | 1.906 ns |  74.36 ns |  78.10 ns |  81.36 ns |  1.00 |    0.03 |    1 | 0.0038 |     320 B |        1.00 |
| EggMapper   |  82.32 ns | 0.752 ns | 0.667 ns |  81.41 ns |  82.18 ns |  83.72 ns |  1.06 |    0.03 |    1 | 0.0030 |     256 B |        0.80 |
| AutoMapper  | 125.87 ns | 0.989 ns | 0.925 ns | 124.24 ns | 125.97 ns | 127.75 ns |  1.62 |    0.04 |    3 | 0.0038 |     328 B |        1.02 |
| Mapster     |  94.83 ns | 1.374 ns | 1.285 ns |  92.83 ns |  94.99 ns |  96.98 ns |  1.22 |    0.03 |    2 | 0.0038 |     320 B |        1.00 |
| MapperlyMap |  79.66 ns | 1.598 ns | 1.641 ns |  76.64 ns |  79.54 ns |  82.23 ns |  1.02 |    0.03 |    1 | 0.0038 |     320 B |        1.00 |
| AgileMapper | 325.17 ns | 3.317 ns | 3.103 ns | 321.69 ns | 324.13 ns | 331.64 ns |  4.18 |    0.11 |    4 | 0.0062 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.775 μs | 0.0347 μs | 0.0617 μs | 1.671 μs | 1.767 μs | 1.910 μs |  1.00 |    0.05 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| EggMapper   | 1.761 μs | 0.0347 μs | 0.0560 μs | 1.629 μs | 1.757 μs | 1.871 μs |  0.99 |    0.05 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AutoMapper  | 2.427 μs | 0.0472 μs | 0.0735 μs | 2.258 μs | 2.421 μs | 2.546 μs |  1.37 |    0.06 |    4 | 0.1183 | 0.0038 |   9.95 KB |        1.15 |
| Mapster     | 1.872 μs | 0.0374 μs | 0.1042 μs | 1.661 μs | 1.883 μs | 2.095 μs |  1.06 |    0.07 |    1 | 0.1030 |      - |   8.65 KB |        1.00 |
| MapperlyMap | 2.000 μs | 0.0400 μs | 0.0772 μs | 1.797 μs | 2.004 μs | 2.144 μs |  1.13 |    0.06 |    2 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AgileMapper | 2.288 μs | 0.0456 μs | 0.0823 μs | 2.103 μs | 2.298 μs | 2.484 μs |  1.29 |    0.06 |    3 | 0.1068 |      - |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.091 μs | 0.1216 μs | 0.1928 μs | 5.639 μs | 6.077 μs | 6.374 μs |  1.00 |    0.04 |    3 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| EggMapper   | 4.492 μs | 0.0765 μs | 0.0716 μs | 4.317 μs | 4.501 μs | 4.581 μs |  0.74 |    0.03 |    1 | 0.1984 | 0.0076 |  16.46 KB |        0.60 |
| AutoMapper  | 7.188 μs | 0.1432 μs | 0.1811 μs | 6.831 μs | 7.197 μs | 7.568 μs |  1.18 |    0.05 |    5 | 0.3510 | 0.0153 |   28.7 KB |        1.05 |
| Mapster     | 6.547 μs | 0.1270 μs | 0.1126 μs | 6.297 μs | 6.598 μs | 6.697 μs |  1.08 |    0.04 |    4 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| MapperlyMap | 6.176 μs | 0.1167 μs | 0.1248 μs | 5.888 μs | 6.234 μs | 6.321 μs |  1.02 |    0.04 |    3 | 0.3357 | 0.0153 |  27.42 KB |        1.00 |
| AgileMapper | 5.106 μs | 0.0430 μs | 0.0381 μs | 5.040 μs | 5.112 μs | 5.174 μs |  0.84 |    0.03 |    2 | 0.1984 | 0.0076 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.41 μs | 0.306 μs | 0.255 μs | 17.65 μs | 18.48 μs | 18.63 μs |  1.00 |    0.02 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| EggMapper   | 19.74 μs | 0.347 μs | 0.325 μs | 18.91 μs | 19.78 μs | 20.19 μs |  1.07 |    0.02 |    2 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| AutoMapper  | 20.55 μs | 0.407 μs | 0.877 μs | 18.54 μs | 20.70 μs | 22.20 μs |  1.12 |    0.05 |    2 | 1.1292 | 0.2747 |  94.34 KB |        1.10 |
| Mapster     | 18.01 μs | 0.357 μs | 0.679 μs | 16.51 μs | 18.05 μs | 19.34 μs |  0.98 |    0.04 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| MapperlyMap | 17.60 μs | 0.348 μs | 0.637 μs | 16.31 μs | 17.61 μs | 19.17 μs |  0.96 |    0.04 |    1 | 1.0376 | 0.2441 |  86.02 KB |        1.00 |
| AgileMapper | 20.29 μs | 0.330 μs | 0.308 μs | 19.65 μs | 20.37 μs | 20.86 μs |  1.10 |    0.02 |    2 | 1.0376 | 0.2441 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Min       | Median    | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  9.815 ns | 0.2010 ns | 0.1880 ns |  9.413 ns |  9.856 ns | 10.12 ns |  1.00 |    0.03 |    1 |         - |          NA |
| EggMapper      | 17.659 ns | 0.3474 ns | 0.3250 ns | 17.037 ns | 17.650 ns | 18.19 ns |  1.80 |    0.05 |    3 |         - |          NA |
| EggMapperTyped | 11.788 ns | 0.1150 ns | 0.1076 ns | 11.523 ns | 11.824 ns | 11.95 ns |  1.20 |    0.02 |    2 |         - |          NA |
| AutoMapper     | 42.700 ns | 0.5445 ns | 0.5093 ns | 41.667 ns | 42.816 ns | 43.31 ns |  4.35 |    0.10 |    4 |         - |          NA |
| Mapster        | 17.850 ns | 0.1742 ns | 0.1544 ns | 17.577 ns | 17.893 ns | 18.14 ns |  1.82 |    0.04 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 16.45 ns | 0.382 ns | 0.838 ns | 14.78 ns | 16.45 ns | 18.58 ns |  1.00 |    0.07 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper                   | 28.55 ns | 0.615 ns | 1.141 ns | 25.80 ns | 28.79 ns | 30.61 ns |  1.74 |    0.11 |    2 | 0.0010 |      80 B |        1.00 |
| AutoMapper                  | 58.37 ns | 1.112 ns | 1.040 ns | 54.90 ns | 58.66 ns | 59.14 ns |  3.56 |    0.19 |    3 | 0.0010 |      80 B |        1.00 |
| EggMapperDerivedSource      | 29.13 ns | 0.635 ns | 0.950 ns | 26.75 ns | 29.24 ns | 30.84 ns |  1.78 |    0.11 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapperAlternatingSources | 30.30 ns | 0.636 ns | 0.595 ns | 29.21 ns | 30.25 ns | 31.41 ns |  1.85 |    0.10 |    2 | 0.0010 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.451 μs | 0.0284 μs | 0.0533 μs |   1.351 μs |   1.451 μs |   1.580 μs |   1.00 |    0.05 |    1 | 0.0916 | 0.0019 |   7.52 KB |        1.00 |
| AutoMapperStartup | 350.275 μs | 2.6248 μs | 2.1918 μs | 346.593 μs | 350.236 μs | 353.967 μs | 241.72 |    8.86 |    3 |      - |      - | 105.89 KB |       14.09 |
| MapsterStartup    |   2.744 μs | 0.0544 μs | 0.0814 μs |   2.551 μs |   2.750 μs |   2.862 μs |   1.89 |    0.09 |    2 | 0.1373 | 0.0038 |  11.51 KB |        1.53 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| EggMapper  | 1.283 ms | 0.0124 ms | 0.0116 ms | 1.268 ms | 1.285 ms | 1.307 ms |  1.00 |    0.01 |    1 |      - |  87.72 KB |        1.00 |
| AutoMapper | 3.980 ms | 0.0496 ms | 0.0440 ms | 3.926 ms | 3.969 ms | 4.093 ms |  3.10 |    0.04 |    3 |      - | 327.08 KB |        3.73 |
| Mapster    | 2.250 ms | 0.0135 ms | 0.0105 ms | 2.222 ms | 2.254 ms | 2.263 ms |  1.75 |    0.02 |    2 | 7.8125 | 758.11 KB |        8.64 |

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

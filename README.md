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
> ⏱ **Last updated:** 2026-11-15 00:25 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 12.28 ns | **21.63 ns (1.8×)** | 22.89 ns (1.9×) | 48.73 ns (4.0×) | 14.87 ns (1.2×) |
| **Flattening** | 16.20 ns | **25.43 ns (1.6×)** | 35.30 ns (2.2×) | 48.46 ns (3.0×) | 23.43 ns (1.4×) |
| **Deep (2 nested)** | 51.04 ns | **44.82 ns (0.9×)** | 65.63 ns (1.3×) | 98.59 ns (1.9×) | 47.31 ns (0.9×) |
| **Complex (nest+coll)** | 74.20 ns | **65.64 ns (0.9×)** | 85.58 ns (1.1×) | 117.65 ns (1.6×) | 68.51 ns (0.9×) |
| **Collection (100)** | 1.452 μs | **1.581 μs (1.1×)** | 1.309 μs (0.9×) | 1.933 μs (1.3×) | 1.687 μs (1.2×) |
| **Deep Coll (100)** | 4.985 μs | **3.939 μs (0.8×)** | 5.944 μs (1.2×) | 6.203 μs (1.2×) | 5.166 μs (1.0×) |
| **Large Coll (1000)** | 16.46 μs | **16.97 μs (1.0×)** | 16.24 μs (1.0×) | 18.11 μs (1.1×) | 16.39 μs (1.0×) |
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
| Manual               |  12.28 ns | 0.297 ns | 0.445 ns |  11.63 ns |  12.19 ns |  13.24 ns |  1.00 |    0.05 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper            |  21.63 ns | 0.471 ns | 1.156 ns |  19.73 ns |  21.53 ns |  24.74 ns |  1.76 |    0.11 |    4 | 0.0010 |      80 B |        1.00 |
| EggMapperTyped       |  17.48 ns | 0.397 ns | 0.912 ns |  15.79 ns |  17.55 ns |  19.60 ns |  1.43 |    0.09 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper           |  48.73 ns | 0.735 ns | 0.652 ns |  47.97 ns |  48.56 ns |  50.53 ns |  3.97 |    0.15 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster              |  22.89 ns | 0.445 ns | 0.666 ns |  21.76 ns |  22.75 ns |  24.40 ns |  1.87 |    0.08 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap          |  14.87 ns | 0.346 ns | 0.462 ns |  14.01 ns |  14.79 ns |  15.88 ns |  1.21 |    0.06 |    2 | 0.0010 |      80 B |        1.00 |
| AgileMapper          | 238.60 ns | 4.361 ns | 7.863 ns | 226.40 ns | 238.94 ns | 253.43 ns | 19.46 |    0.93 |    6 | 0.0041 |     344 B |        4.30 |
| EggMapperGenerator   |  15.31 ns | 0.423 ns | 1.246 ns |  13.26 ns |  15.29 ns |  18.11 ns |  1.25 |    0.11 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapperClassMapper |  15.36 ns | 0.354 ns | 0.761 ns |  14.15 ns |  15.35 ns |  17.30 ns |  1.25 |    0.08 |    2 | 0.0010 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  16.20 ns | 0.311 ns | 0.426 ns |  15.59 ns |  16.09 ns |  17.33 ns |  1.00 |    0.04 |    1 | 0.0010 |      80 B |        1.00 |
| EggMap      |  25.43 ns | 0.507 ns | 1.310 ns |  23.41 ns |  25.44 ns |  28.97 ns |  1.57 |    0.09 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper  |  48.46 ns | 0.620 ns | 0.517 ns |  47.76 ns |  48.48 ns |  49.57 ns |  2.99 |    0.08 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster     |  35.30 ns | 0.697 ns | 0.544 ns |  34.30 ns |  35.38 ns |  36.06 ns |  2.18 |    0.06 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap |  23.43 ns | 0.514 ns | 1.251 ns |  21.11 ns |  23.58 ns |  26.02 ns |  1.45 |    0.09 |    2 | 0.0012 |     104 B |        1.30 |
| AgileMapper | 247.38 ns | 4.641 ns | 4.114 ns | 237.58 ns | 247.41 ns | 252.13 ns | 15.28 |    0.46 |    6 | 0.0038 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  51.04 ns | 0.928 ns |  1.239 ns |  50.99 ns |  48.82 ns |  54.10 ns |  1.00 |    0.03 |    1 | 0.0032 |     272 B |        1.00 |
| EggMapper   |  44.82 ns | 0.942 ns |  1.987 ns |  44.71 ns |  41.51 ns |  48.69 ns |  0.88 |    0.04 |    1 | 0.0019 |     160 B |        0.59 |
| AutoMapper  |  98.59 ns | 2.564 ns |  7.560 ns | 101.43 ns |  77.33 ns | 108.78 ns |  1.93 |    0.15 |    3 | 0.0032 |     272 B |        1.00 |
| Mapster     |  65.63 ns | 1.284 ns |  2.315 ns |  66.05 ns |  59.90 ns |  69.07 ns |  1.29 |    0.05 |    2 | 0.0032 |     272 B |        1.00 |
| MapperlyMap |  47.31 ns | 1.543 ns |  4.548 ns |  46.04 ns |  40.88 ns |  58.29 ns |  0.93 |    0.09 |    1 | 0.0032 |     272 B |        1.00 |
| AgileMapper | 252.74 ns | 5.048 ns | 13.475 ns | 248.57 ns | 236.62 ns | 288.68 ns |  4.95 |    0.29 |    4 | 0.0048 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  74.20 ns | 1.374 ns | 1.147 ns |  71.29 ns |  74.56 ns |  75.35 ns |  1.00 |    0.02 |    2 | 0.0038 |     320 B |        1.00 |
| EggMapper   |  65.64 ns | 0.540 ns | 0.479 ns |  64.35 ns |  65.77 ns |  66.10 ns |  0.88 |    0.01 |    1 | 0.0030 |     256 B |        0.80 |
| AutoMapper  | 117.65 ns | 2.288 ns | 2.248 ns | 113.68 ns | 118.14 ns | 121.08 ns |  1.59 |    0.04 |    4 | 0.0038 |     328 B |        1.02 |
| Mapster     |  85.58 ns | 1.704 ns | 4.015 ns |  77.66 ns |  85.41 ns |  94.93 ns |  1.15 |    0.06 |    3 | 0.0038 |     320 B |        1.00 |
| MapperlyMap |  68.51 ns | 1.405 ns | 2.708 ns |  63.40 ns |  68.19 ns |  73.31 ns |  0.92 |    0.04 |    1 | 0.0038 |     320 B |        1.00 |
| AgileMapper | 309.17 ns | 4.866 ns | 4.063 ns | 302.15 ns | 308.35 ns | 316.19 ns |  4.17 |    0.08 |    5 | 0.0062 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.452 μs | 0.0368 μs | 0.1085 μs | 1.271 μs | 1.429 μs | 1.754 μs |  1.01 |    0.10 |    2 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| EggMapper   | 1.581 μs | 0.0470 μs | 0.1387 μs | 1.320 μs | 1.606 μs | 1.936 μs |  1.09 |    0.12 |    3 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AutoMapper  | 1.933 μs | 0.0382 μs | 0.0822 μs | 1.811 μs | 1.921 μs | 2.123 μs |  1.34 |    0.11 |    4 | 0.1183 | 0.0038 |   9.95 KB |        1.15 |
| Mapster     | 1.309 μs | 0.0252 μs | 0.0235 μs | 1.275 μs | 1.304 μs | 1.351 μs |  0.91 |    0.07 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| MapperlyMap | 1.687 μs | 0.0522 μs | 0.1540 μs | 1.350 μs | 1.719 μs | 1.976 μs |  1.17 |    0.14 |    3 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AgileMapper | 2.185 μs | 0.0330 μs | 0.0405 μs | 2.117 μs | 2.184 μs | 2.260 μs |  1.51 |    0.11 |    5 | 0.1068 |      - |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.985 μs | 0.0984 μs | 0.1824 μs | 4.488 μs | 5.003 μs | 5.257 μs |  1.00 |    0.05 |    3 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| EggMapper   | 3.939 μs | 0.0776 μs | 0.1317 μs | 3.729 μs | 3.915 μs | 4.292 μs |  0.79 |    0.04 |    1 | 0.1984 | 0.0114 |  16.46 KB |        0.60 |
| AutoMapper  | 6.203 μs | 0.1169 μs | 0.1887 μs | 5.877 μs | 6.168 μs | 6.657 μs |  1.25 |    0.06 |    4 | 0.3510 | 0.0153 |   28.7 KB |        1.05 |
| Mapster     | 5.944 μs | 0.1178 μs | 0.2890 μs | 5.315 μs | 5.924 μs | 6.540 μs |  1.19 |    0.07 |    4 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| MapperlyMap | 5.166 μs | 0.1025 μs | 0.1566 μs | 4.860 μs | 5.165 μs | 5.517 μs |  1.04 |    0.05 |    3 | 0.3357 | 0.0153 |  27.42 KB |        1.00 |
| AgileMapper | 4.223 μs | 0.0755 μs | 0.0669 μs | 4.146 μs | 4.207 μs | 4.408 μs |  0.85 |    0.03 |    2 | 0.1984 | 0.0076 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 16.46 μs | 0.326 μs | 0.666 μs | 15.03 μs | 16.46 μs | 17.98 μs |  1.00 |    0.06 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| EggMapper   | 16.97 μs | 0.333 μs | 0.575 μs | 15.63 μs | 17.10 μs | 18.09 μs |  1.03 |    0.05 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| AutoMapper  | 18.11 μs | 0.361 μs | 0.893 μs | 16.23 μs | 18.08 μs | 20.30 μs |  1.10 |    0.07 |    2 | 1.1292 | 0.2747 |  94.34 KB |        1.10 |
| Mapster     | 16.24 μs | 0.323 μs | 0.623 μs | 15.07 μs | 16.24 μs | 17.46 μs |  0.99 |    0.05 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| MapperlyMap | 16.39 μs | 0.326 μs | 0.554 μs | 15.07 μs | 16.36 μs | 17.31 μs |  1.00 |    0.05 |    1 | 1.0529 | 0.2594 |  86.02 KB |        1.00 |
| AgileMapper | 19.69 μs | 0.377 μs | 0.812 μs | 18.63 μs | 19.49 μs | 21.55 μs |  1.20 |    0.07 |    3 | 1.0376 | 0.2441 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  8.888 ns | 0.1296 ns | 0.1149 ns |  8.870 ns |  8.737 ns |  9.126 ns |  1.00 |    0.02 |    1 |         - |          NA |
| EggMapper      | 15.830 ns | 0.1948 ns | 0.1627 ns | 15.743 ns | 15.689 ns | 16.159 ns |  1.78 |    0.03 |    3 |         - |          NA |
| EggMapperTyped | 10.371 ns | 0.0465 ns | 0.0388 ns | 10.387 ns | 10.265 ns | 10.398 ns |  1.17 |    0.02 |    2 |         - |          NA |
| AutoMapper     | 34.960 ns | 0.1591 ns | 0.1242 ns | 34.964 ns | 34.698 ns | 35.149 ns |  3.93 |    0.05 |    4 |         - |          NA |
| Mapster        | 15.673 ns | 0.3098 ns | 0.6734 ns | 15.289 ns | 15.003 ns | 17.160 ns |  1.76 |    0.08 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 15.63 ns | 0.355 ns | 0.462 ns | 14.99 ns | 15.55 ns | 16.62 ns |  1.00 |    0.04 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper                   | 27.53 ns | 0.559 ns | 0.523 ns | 26.25 ns | 27.47 ns | 28.33 ns |  1.76 |    0.06 |    2 | 0.0010 |      80 B |        1.00 |
| AutoMapper                  | 46.80 ns | 0.403 ns | 0.315 ns | 46.41 ns | 46.74 ns | 47.44 ns |  3.00 |    0.09 |    3 | 0.0010 |      80 B |        1.00 |
| EggMapperDerivedSource      | 26.68 ns | 0.561 ns | 0.768 ns | 25.56 ns | 26.50 ns | 28.58 ns |  1.71 |    0.07 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapperAlternatingSources | 27.16 ns | 0.422 ns | 0.374 ns | 26.60 ns | 27.05 ns | 27.86 ns |  1.74 |    0.05 |    2 | 0.0010 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.290 μs | 0.0256 μs | 0.0494 μs |   1.186 μs |   1.287 μs |   1.404 μs |   1.00 |    0.05 |    1 | 0.0916 | 0.0019 |   7.52 KB |        1.00 |
| AutoMapperStartup | 319.136 μs | 2.7943 μs | 2.6138 μs | 315.266 μs | 319.427 μs | 324.725 μs | 247.81 |    9.51 |    3 |      - |      - | 106.18 KB |       14.13 |
| MapsterStartup    |   2.490 μs | 0.0457 μs | 0.0697 μs |   2.374 μs |   2.475 μs |   2.637 μs |   1.93 |    0.09 |    2 | 0.1373 | 0.0038 |  11.51 KB |        1.53 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| EggMapper  | 1.130 ms | 0.0165 ms | 0.0146 ms | 1.127 ms | 1.108 ms | 1.158 ms |  1.00 |    0.02 |    1 |      - |   87.8 KB |        1.00 |
| AutoMapper | 3.601 ms | 0.0612 ms | 0.0897 ms | 3.575 ms | 3.495 ms | 3.863 ms |  3.19 |    0.09 |    3 |      - |    327 KB |        3.72 |
| Mapster    | 2.046 ms | 0.0409 ms | 0.0853 ms | 2.010 ms | 1.946 ms | 2.262 ms |  1.81 |    0.08 |    2 | 7.8125 | 764.87 KB |        8.71 |

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

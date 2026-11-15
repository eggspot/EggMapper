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

Optional build-time checks: `dotnet add package EggMapper.Analyzers` reports every destination member of a
`CreateMap` that has no source member while you build, with the `ForMember` fix in the message
([EGG1004](https://eggspot.github.io/EggMapper/diagnostics/EGG1004.html)). It also warns when a `MapperConfiguration`
is never validated ([EGG1002](https://eggspot.github.io/EggMapper/diagnostics/EGG1002.html)) and suggests
compile-time `[MapTo]` for maps without customization
([EGG1003](https://eggspot.github.io/EggMapper/diagnostics/EGG1003.html)).

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
> ⏱ **Last updated:** 2026-11-15 00:47 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 20.70 ns | **30.39 ns (1.5×)** | 30.81 ns (1.5×) | 71.91 ns (3.5×) | 19.13 ns (0.9×) |
| **Flattening** | 21.88 ns | **32.49 ns (1.5×)** | 41.95 ns (1.9×) | 75.04 ns (3.4×) | 29.40 ns (1.3×) |
| **Deep (2 nested)** | 67.71 ns | **63.48 ns (0.9×)** | 81.38 ns (1.2×) | 124.49 ns (1.8×) | 63.36 ns (0.9×) |
| **Complex (nest+coll)** | 77.22 ns | **84.09 ns (1.1×)** | 97.67 ns (1.3×) | 143.62 ns (1.9×) | 80.42 ns (1.0×) |
| **Collection (100)** | 2.020 μs | **1.886 μs (0.9×)** | 1.997 μs (1.0×) | 2.707 μs (1.3×) | 2.064 μs (1.0×) |
| **Deep Coll (100)** | 6.573 μs | **5.327 μs (0.8×)** | 7.426 μs (1.1×) | 8.068 μs (1.2×) | 6.891 μs (1.1×) |
| **Large Coll (1000)** | 19.61 μs | **21.30 μs (1.1×)** | 19.43 μs (1.0×) | 24.26 μs (1.2×) | 22.52 μs (1.1×) |
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
| Manual               |  20.70 ns | 0.312 ns | 0.291 ns |  20.24 ns |  20.62 ns |  21.22 ns |  1.00 |    0.02 |    2 | 0.0032 |      80 B |        1.00 |
| EggMapper            |  30.39 ns | 0.468 ns | 0.437 ns |  29.48 ns |  30.31 ns |  31.30 ns |  1.47 |    0.03 |    4 | 0.0032 |      80 B |        1.00 |
| EggMapperTyped       |  23.57 ns | 0.223 ns | 0.197 ns |  23.27 ns |  23.66 ns |  23.87 ns |  1.14 |    0.02 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper           |  71.91 ns | 0.311 ns | 0.291 ns |  71.42 ns |  71.84 ns |  72.52 ns |  3.47 |    0.05 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster              |  30.81 ns | 0.180 ns | 0.160 ns |  30.53 ns |  30.84 ns |  31.14 ns |  1.49 |    0.02 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap          |  19.13 ns | 0.381 ns | 0.357 ns |  18.62 ns |  19.15 ns |  19.68 ns |  0.92 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| AgileMapper          | 365.05 ns | 1.590 ns | 1.487 ns | 363.50 ns | 364.88 ns | 368.15 ns | 17.64 |    0.25 |    6 | 0.0134 |     344 B |        4.30 |
| EggMapperGenerator   |  19.19 ns | 0.457 ns | 0.562 ns |  17.75 ns |  19.31 ns |  19.86 ns |  0.93 |    0.03 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapperClassMapper |  19.56 ns | 0.460 ns | 0.408 ns |  18.69 ns |  19.66 ns |  20.02 ns |  0.95 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  21.88 ns | 0.518 ns | 0.532 ns |  20.74 ns |  22.16 ns |  22.56 ns |  1.00 |    0.03 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  32.49 ns | 0.447 ns | 0.418 ns |  31.87 ns |  32.56 ns |  33.19 ns |  1.49 |    0.04 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  75.04 ns | 0.634 ns | 0.593 ns |  74.12 ns |  75.17 ns |  76.10 ns |  3.43 |    0.09 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  41.95 ns | 0.275 ns | 0.244 ns |  41.60 ns |  42.00 ns |  42.47 ns |  1.92 |    0.05 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  29.40 ns | 0.651 ns | 0.750 ns |  27.54 ns |  29.66 ns |  30.31 ns |  1.34 |    0.05 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 364.82 ns | 0.519 ns | 0.486 ns | 364.03 ns | 364.85 ns | 365.73 ns | 16.68 |    0.40 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  67.71 ns | 0.575 ns | 0.510 ns |  67.07 ns |  67.66 ns |  68.50 ns |  1.00 |    0.01 |    2 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  63.48 ns | 0.426 ns | 0.399 ns |  62.73 ns |  63.42 ns |  64.18 ns |  0.94 |    0.01 |    1 | 0.0063 |     160 B |        0.59 |
| AutoMapper  | 124.49 ns | 0.865 ns | 0.809 ns | 123.25 ns | 124.33 ns | 125.88 ns |  1.84 |    0.02 |    4 | 0.0107 |     272 B |        1.00 |
| Mapster     |  81.38 ns | 1.130 ns | 1.057 ns |  79.86 ns |  81.01 ns |  83.31 ns |  1.20 |    0.02 |    3 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  63.36 ns | 0.801 ns | 0.626 ns |  61.65 ns |  63.51 ns |  64.04 ns |  0.94 |    0.01 |    1 | 0.0107 |     272 B |        1.00 |
| AgileMapper | 399.50 ns | 2.053 ns | 1.920 ns | 397.12 ns | 399.48 ns | 403.64 ns |  5.90 |    0.05 |    5 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  77.22 ns | 1.133 ns | 1.004 ns |  75.88 ns |  77.24 ns |  79.23 ns |  1.00 |    0.02 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   |  84.09 ns | 0.995 ns | 0.931 ns |  82.49 ns |  84.22 ns |  85.91 ns |  1.09 |    0.02 |    1 | 0.0101 |     256 B |        0.80 |
| AutoMapper  | 143.62 ns | 1.131 ns | 1.002 ns | 142.35 ns | 143.42 ns | 145.96 ns |  1.86 |    0.03 |    3 | 0.0129 |     328 B |        1.02 |
| Mapster     |  97.67 ns | 1.344 ns | 1.257 ns |  94.90 ns |  97.44 ns |  99.84 ns |  1.26 |    0.02 |    2 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  80.42 ns | 1.660 ns | 3.537 ns |  73.88 ns |  80.18 ns |  86.71 ns |  1.04 |    0.05 |    1 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 452.64 ns | 1.960 ns | 1.834 ns | 447.94 ns | 452.46 ns | 456.00 ns |  5.86 |    0.08 |    4 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.020 μs | 0.0387 μs | 0.0362 μs | 1.964 μs | 2.024 μs | 2.091 μs |  1.00 |    0.02 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| EggMapper   | 1.886 μs | 0.0376 μs | 0.0476 μs | 1.817 μs | 1.881 μs | 1.967 μs |  0.93 |    0.03 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| AutoMapper  | 2.707 μs | 0.0502 μs | 0.0445 μs | 2.631 μs | 2.711 μs | 2.780 μs |  1.34 |    0.03 |    3 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.997 μs | 0.0352 μs | 0.0312 μs | 1.949 μs | 1.994 μs | 2.067 μs |  0.99 |    0.02 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| MapperlyMap | 2.064 μs | 0.0359 μs | 0.0318 μs | 1.994 μs | 2.067 μs | 2.119 μs |  1.02 |    0.02 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AgileMapper | 2.571 μs | 0.0276 μs | 0.0230 μs | 2.527 μs | 2.571 μs | 2.603 μs |  1.27 |    0.02 |    2 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.573 μs | 0.1309 μs | 0.3261 μs | 5.868 μs | 6.605 μs | 7.358 μs |  1.00 |    0.07 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 5.327 μs | 0.1059 μs | 0.0938 μs | 5.082 μs | 5.341 μs | 5.462 μs |  0.81 |    0.04 |    1 | 0.6714 | 0.0381 |  16.46 KB |        0.60 |
| AutoMapper  | 8.068 μs | 0.1591 μs | 0.2124 μs | 7.495 μs | 8.106 μs | 8.412 μs |  1.23 |    0.07 |    4 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 7.426 μs | 0.0907 μs | 0.0757 μs | 7.289 μs | 7.446 μs | 7.509 μs |  1.13 |    0.06 |    3 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 6.891 μs | 0.1085 μs | 0.1015 μs | 6.723 μs | 6.887 μs | 7.077 μs |  1.05 |    0.05 |    2 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 6.281 μs | 0.1001 μs | 0.0936 μs | 6.129 μs | 6.282 μs | 6.422 μs |  0.96 |    0.05 |    2 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.61 μs | 0.390 μs | 0.672 μs | 18.44 μs | 19.50 μs | 20.85 μs |  1.00 |    0.05 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 21.30 μs | 0.402 μs | 0.430 μs | 20.75 μs | 21.25 μs | 22.22 μs |  1.09 |    0.04 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 24.26 μs | 0.483 μs | 0.629 μs | 23.20 μs | 24.16 μs | 25.41 μs |  1.24 |    0.05 |    2 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 19.43 μs | 0.387 μs | 0.380 μs | 18.84 μs | 19.37 μs | 20.07 μs |  0.99 |    0.04 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 22.52 μs | 0.449 μs | 0.699 μs | 21.20 μs | 22.39 μs | 23.77 μs |  1.15 |    0.05 |    2 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 23.13 μs | 0.442 μs | 0.509 μs | 22.19 μs | 23.01 μs | 24.20 μs |  1.18 |    0.05 |    2 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         | 13.83 ns | 0.240 ns | 0.224 ns | 13.30 ns | 13.83 ns | 14.06 ns |  1.00 |    0.02 |    1 |         - |          NA |
| EggMapper      | 23.73 ns | 0.043 ns | 0.040 ns | 23.67 ns | 23.73 ns | 23.78 ns |  1.72 |    0.03 |    3 |         - |          NA |
| EggMapperTyped | 18.94 ns | 0.027 ns | 0.025 ns | 18.90 ns | 18.94 ns | 18.98 ns |  1.37 |    0.02 |    2 |         - |          NA |
| AutoMapper     | 57.38 ns | 0.058 ns | 0.049 ns | 57.26 ns | 57.39 ns | 57.43 ns |  4.15 |    0.07 |    5 |         - |          NA |
| Mapster        | 24.47 ns | 0.076 ns | 0.071 ns | 24.24 ns | 24.49 ns | 24.53 ns |  1.77 |    0.03 |    4 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 17.40 ns | 0.419 ns | 0.677 ns | 16.16 ns | 17.28 ns | 18.96 ns |  1.00 |    0.05 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapper                   | 34.44 ns | 0.665 ns | 0.622 ns | 33.00 ns | 34.48 ns | 35.17 ns |  1.98 |    0.08 |    2 | 0.0032 |      80 B |        1.00 |
| AutoMapper                  | 72.08 ns | 0.276 ns | 0.216 ns | 71.60 ns | 72.15 ns | 72.35 ns |  4.15 |    0.16 |    4 | 0.0031 |      80 B |        1.00 |
| EggMapperDerivedSource      | 37.87 ns | 0.588 ns | 0.550 ns | 37.19 ns | 37.83 ns | 38.80 ns |  2.18 |    0.09 |    3 | 0.0032 |      80 B |        1.00 |
| EggMapperAlternatingSources | 39.48 ns | 0.619 ns | 0.579 ns | 38.36 ns | 39.53 ns | 40.27 ns |  2.27 |    0.09 |    3 | 0.0032 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.613 μs | 0.0321 μs | 0.0725 μs |   1.420 μs |   1.627 μs |   1.754 μs |   1.00 |    0.06 |    1 | 0.3090 | 0.0057 |   7.59 KB |        1.00 |
| AutoMapperStartup | 402.801 μs | 2.7989 μs | 2.3372 μs | 400.207 μs | 402.377 μs | 408.507 μs | 250.22 |   11.74 |    3 | 3.9063 |      - | 106.73 KB |       14.06 |
| MapsterStartup    |   3.164 μs | 0.0522 μs | 0.0463 μs |   3.065 μs |   3.176 μs |   3.207 μs |   1.97 |    0.10 |    2 | 0.4692 | 0.0114 |  11.51 KB |        1.52 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.454 ms | 0.0140 ms | 0.0131 ms | 1.421 ms | 1.456 ms | 1.476 ms |  1.00 |    0.01 |    1 |       - |       - |   87.1 KB |        1.00 |
| AutoMapper | 4.654 ms | 0.0448 ms | 0.0419 ms | 4.599 ms | 4.643 ms | 4.724 ms |  3.20 |    0.04 |    3 |  7.8125 |       - |  327.3 KB |        3.76 |
| Mapster    | 2.494 ms | 0.0184 ms | 0.0172 ms | 2.475 ms | 2.487 ms | 2.524 ms |  1.72 |    0.02 |    2 | 23.4375 | 15.6250 | 763.48 KB |        8.77 |

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
- ✅ Unmapped destination members reported at build time with the fix (EGG1004, `EggMapper.Analyzers`)
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
- Reference EggMapper.Analyzers: EGG1004 build warnings name the ForMember fix for each unmapped member; apply them.
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

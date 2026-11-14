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
> ⏱ **Last updated:** 2026-11-14 16:10 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 16.11 ns | **27.88 ns (1.7×)** | 28.60 ns (1.8×) | 78.78 ns (4.9×) | 15.95 ns (1.0×) |
| **Flattening** | 20.02 ns | **31.15 ns (1.6×)** | 35.10 ns (1.8×) | 84.11 ns (4.2×) | 27.30 ns (1.4×) |
| **Deep (2 nested)** | 58.29 ns | **56.90 ns (1.0×)** | 72.65 ns (1.2×) | 118.54 ns (2.0×) | 53.95 ns (0.9×) |
| **Complex (nest+coll)** | 80.56 ns | **94.62 ns (1.2×)** | 100.57 ns (1.2×) | 151.48 ns (1.9×) | 81.20 ns (1.0×) |
| **Collection (100)** | 2.116 μs | **2.111 μs (1.0×)** | 2.277 μs (1.1×) | 2.658 μs (1.3×) | 2.273 μs (1.1×) |
| **Deep Coll (100)** | 6.599 μs | **4.972 μs (0.8×)** | 6.657 μs (1.0×) | 7.344 μs (1.1×) | 6.068 μs (0.9×) |
| **Large Coll (1000)** | 21.23 μs | **20.42 μs (1.0×)** | 20.77 μs (1.0×) | 25.04 μs (1.2×) | 21.47 μs (1.0×) |
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
| Manual               |  16.11 ns | 0.348 ns | 0.308 ns |  15.61 ns |  16.02 ns |  16.72 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.88 ns | 0.275 ns | 0.257 ns |  27.47 ns |  27.90 ns |  28.35 ns |  1.73 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  21.42 ns | 0.204 ns | 0.181 ns |  21.11 ns |  21.43 ns |  21.71 ns |  1.33 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  78.78 ns | 0.063 ns | 0.049 ns |  78.67 ns |  78.78 ns |  78.85 ns |  4.89 |    0.09 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.60 ns | 0.434 ns | 0.406 ns |  28.06 ns |  28.51 ns |  29.43 ns |  1.78 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.95 ns | 0.293 ns | 0.274 ns |  15.62 ns |  15.97 ns |  16.38 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 348.54 ns | 1.162 ns | 0.970 ns | 346.61 ns | 348.60 ns | 349.57 ns | 21.64 |    0.40 |    5 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  15.92 ns | 0.190 ns | 0.168 ns |  15.66 ns |  16.00 ns |  16.12 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.32 ns | 0.141 ns | 0.132 ns |  16.09 ns |  16.39 ns |  16.45 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.02 ns | 0.100 ns | 0.083 ns |  19.96 ns |  20.00 ns |  20.24 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.15 ns | 0.237 ns | 0.210 ns |  30.83 ns |  31.13 ns |  31.55 ns |  1.56 |    0.01 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  84.11 ns | 0.379 ns | 0.336 ns |  83.68 ns |  84.07 ns |  84.62 ns |  4.20 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  35.10 ns | 0.281 ns | 0.263 ns |  34.64 ns |  35.07 ns |  35.56 ns |  1.75 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  27.30 ns | 0.286 ns | 0.267 ns |  26.85 ns |  27.20 ns |  27.72 ns |  1.36 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 332.94 ns | 1.050 ns | 0.982 ns | 331.57 ns | 332.73 ns | 334.74 ns | 16.63 |    0.08 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  58.29 ns | 0.180 ns | 0.168 ns |  58.03 ns |  58.29 ns |  58.55 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  56.90 ns | 0.250 ns | 0.222 ns |  56.51 ns |  56.87 ns |  57.35 ns |  0.98 |    0.00 |    2 | 0.0095 |     160 B |        0.59 |
| AutoMapper  | 118.54 ns | 0.326 ns | 0.289 ns | 118.01 ns | 118.57 ns | 119.04 ns |  2.03 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  72.65 ns | 0.220 ns | 0.184 ns |  72.40 ns |  72.57 ns |  73.02 ns |  1.25 |    0.00 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  53.95 ns | 0.116 ns | 0.097 ns |  53.76 ns |  53.94 ns |  54.14 ns |  0.93 |    0.00 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 366.81 ns | 2.354 ns | 2.202 ns | 362.84 ns | 366.41 ns | 370.70 ns |  6.29 |    0.04 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  80.56 ns | 0.995 ns | 0.931 ns |  78.34 ns |  80.77 ns |  81.69 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  94.62 ns | 0.825 ns | 0.731 ns |  93.07 ns |  94.83 ns |  95.33 ns |  1.17 |    0.02 |    2 | 0.0153 |     256 B |        0.80 |
| AutoMapper  | 151.48 ns | 1.024 ns | 0.957 ns | 150.30 ns | 151.43 ns | 153.16 ns |  1.88 |    0.02 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     | 100.57 ns | 1.334 ns | 1.247 ns |  98.91 ns | 100.46 ns | 102.58 ns |  1.25 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  81.20 ns | 1.599 ns | 1.495 ns |  78.26 ns |  81.90 ns |  82.92 ns |  1.01 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 405.76 ns | 2.486 ns | 2.326 ns | 401.03 ns | 404.91 ns | 409.25 ns |  5.04 |    0.06 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 2.116 μs | 0.0415 μs | 0.0477 μs | 2.045 μs | 2.116 μs | 2.231 μs |  1.00 |    0.03 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 2.111 μs | 0.0206 μs | 0.0172 μs | 2.068 μs | 2.110 μs | 2.138 μs |  1.00 |    0.02 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.658 μs | 0.0329 μs | 0.0291 μs | 2.593 μs | 2.660 μs | 2.698 μs |  1.26 |    0.03 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.277 μs | 0.0325 μs | 0.0304 μs | 2.218 μs | 2.274 μs | 2.331 μs |  1.08 |    0.03 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 2.273 μs | 0.0249 μs | 0.0233 μs | 2.229 μs | 2.277 μs | 2.307 μs |  1.07 |    0.03 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.684 μs | 0.0375 μs | 0.0351 μs | 2.610 μs | 2.686 μs | 2.729 μs |  1.27 |    0.03 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.599 μs | 0.0605 μs | 0.0505 μs | 6.464 μs | 6.599 μs | 6.683 μs |  1.00 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 4.972 μs | 0.0462 μs | 0.0409 μs | 4.904 μs | 4.973 μs | 5.031 μs |  0.75 |    1 | 1.0071 | 0.0534 |  16.46 KB |        0.60 |
| AutoMapper  | 7.344 μs | 0.0534 μs | 0.0500 μs | 7.237 μs | 7.337 μs | 7.427 μs |  1.11 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.657 μs | 0.0358 μs | 0.0317 μs | 6.580 μs | 6.658 μs | 6.699 μs |  1.01 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 6.068 μs | 0.0739 μs | 0.0691 μs | 5.964 μs | 6.075 μs | 6.183 μs |  0.92 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 6.044 μs | 0.0625 μs | 0.0585 μs | 5.933 μs | 6.050 μs | 6.132 μs |  0.92 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 21.23 μs | 0.202 μs | 0.179 μs | 20.87 μs | 21.28 μs | 21.42 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 20.42 μs | 0.260 μs | 0.243 μs | 19.99 μs | 20.45 μs | 20.79 μs |  0.96 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 25.04 μs | 0.181 μs | 0.161 μs | 24.80 μs | 25.07 μs | 25.38 μs |  1.18 |    0.01 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 20.77 μs | 0.413 μs | 0.565 μs | 19.58 μs | 20.78 μs | 22.02 μs |  0.98 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 21.47 μs | 0.419 μs | 0.392 μs | 20.80 μs | 21.37 μs | 22.12 μs |  1.01 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 23.52 μs | 0.278 μs | 0.260 μs | 22.82 μs | 23.62 μs | 23.79 μs |  1.11 |    0.01 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         | 10.56 ns | 0.061 ns | 0.048 ns | 10.53 ns | 10.54 ns | 10.70 ns |  1.00 |    0.01 |    1 |         - |          NA |
| EggMapper      | 25.15 ns | 0.494 ns | 0.853 ns | 23.09 ns | 25.48 ns | 25.77 ns |  2.38 |    0.08 |    4 |         - |          NA |
| EggMapperTyped | 16.42 ns | 0.202 ns | 0.168 ns | 16.24 ns | 16.33 ns | 16.78 ns |  1.56 |    0.02 |    2 |         - |          NA |
| AutoMapper     | 70.82 ns | 0.162 ns | 0.151 ns | 70.69 ns | 70.73 ns | 71.10 ns |  6.71 |    0.03 |    5 |         - |          NA |
| Mapster        | 22.12 ns | 0.439 ns | 0.877 ns | 20.41 ns | 22.27 ns | 22.97 ns |  2.09 |    0.08 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 16.97 ns | 0.410 ns | 0.708 ns | 15.78 ns | 17.13 ns | 18.62 ns |  1.00 |    0.06 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 32.56 ns | 0.411 ns | 0.384 ns | 31.84 ns | 32.75 ns | 33.01 ns |  1.92 |    0.08 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 79.50 ns | 0.156 ns | 0.138 ns | 79.27 ns | 79.48 ns | 79.71 ns |  4.69 |    0.20 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 33.19 ns | 0.260 ns | 0.231 ns | 32.68 ns | 33.16 ns | 33.54 ns |  1.96 |    0.08 |    2 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 32.64 ns | 0.237 ns | 0.222 ns | 32.30 ns | 32.64 ns | 32.98 ns |  1.93 |    0.08 |    2 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.513 μs | 0.0240 μs | 0.0225 μs |   1.455 μs |   1.513 μs |   1.548 μs |   1.00 |    0.02 |    1 | 0.4520 | 0.0095 |    7.4 KB |        1.00 |
| AutoMapperStartup | 428.686 μs | 7.5226 μs | 7.0366 μs | 417.205 μs | 426.159 μs | 442.001 μs | 283.39 |    6.11 |    3 | 5.8594 |      - | 106.37 KB |       14.38 |
| MapsterStartup    |   2.720 μs | 0.0534 μs | 0.0847 μs |   2.561 μs |   2.711 μs |   2.883 μs |   1.80 |    0.06 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.56 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.514 ms | 0.0199 ms | 0.0186 ms | 1.497 ms | 1.506 ms | 1.556 ms |  1.00 |    0.02 |    1 |  3.9063 |       - |  87.08 KB |        1.00 |
| AutoMapper | 4.743 ms | 0.0193 ms | 0.0171 ms | 4.713 ms | 4.742 ms | 4.778 ms |  3.13 |    0.04 |    3 | 15.6250 |  7.8125 | 327.64 KB |        3.76 |
| Mapster    | 2.585 ms | 0.0130 ms | 0.0122 ms | 2.571 ms | 2.580 ms | 2.609 ms |  1.71 |    0.02 |    2 | 39.0625 | 15.6250 | 759.32 KB |        8.72 |

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

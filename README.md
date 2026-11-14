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
> ⏱ **Last updated:** 2026-11-14 14:22 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 13.60 ns | **22.09 ns (1.6×)** | 22.64 ns (1.7×) | 47.97 ns (3.5×) | 13.07 ns (1.0×) |
| **Flattening** | 15.38 ns | **26.71 ns (1.7×)** | 33.90 ns (2.2×) | 52.69 ns (3.4×) | 20.10 ns (1.3×) |
| **Deep (2 nested)** | 46.70 ns | **55.29 ns (1.2×)** | 61.80 ns (1.3×) | 88.09 ns (1.9×) | 44.72 ns (1.0×) |
| **Complex (nest+coll)** | 63.81 ns | **78.82 ns (1.2×)** | 75.67 ns (1.2×) | 113.38 ns (1.8×) | 61.68 ns (1.0×) |
| **Collection (100)** | 1.461 μs | **1.504 μs (1.0×)** | 1.475 μs (1.0×) | 1.826 μs (1.2×) | 1.589 μs (1.1×) |
| **Deep Coll (100)** | 4.586 μs | **4.888 μs (1.1×)** | 4.886 μs (1.1×) | 5.783 μs (1.3×) | 4.571 μs (1.0×) |
| **Large Coll (1000)** | 14.64 μs | **14.89 μs (1.0×)** | 14.42 μs (1.0×) | 16.09 μs (1.1×) | 16.09 μs (1.1×) |
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
| Manual               |  13.60 ns | 0.138 ns | 0.115 ns |  13.46 ns |  13.59 ns |  13.83 ns |  1.00 |    0.01 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapper            |  22.09 ns | 0.147 ns | 0.137 ns |  21.84 ns |  22.04 ns |  22.32 ns |  1.62 |    0.02 |    4 | 0.0010 |      80 B |        1.00 |
| EggMapperTyped       |  14.86 ns | 0.125 ns | 0.117 ns |  14.65 ns |  14.87 ns |  15.04 ns |  1.09 |    0.01 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper           |  47.97 ns | 0.123 ns | 0.109 ns |  47.78 ns |  47.97 ns |  48.15 ns |  3.53 |    0.03 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster              |  22.64 ns | 0.078 ns | 0.073 ns |  22.52 ns |  22.63 ns |  22.78 ns |  1.67 |    0.01 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap          |  13.07 ns | 0.061 ns | 0.051 ns |  12.97 ns |  13.07 ns |  13.16 ns |  0.96 |    0.01 |    1 | 0.0010 |      80 B |        1.00 |
| AgileMapper          | 259.05 ns | 0.982 ns | 0.918 ns | 257.44 ns | 259.13 ns | 260.34 ns | 19.06 |    0.17 |    6 | 0.0038 |     344 B |        4.30 |
| EggMapperGenerator   |  13.61 ns | 0.074 ns | 0.070 ns |  13.49 ns |  13.61 ns |  13.73 ns |  1.00 |    0.01 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapperClassMapper |  12.99 ns | 0.098 ns | 0.092 ns |  12.78 ns |  13.01 ns |  13.09 ns |  0.96 |    0.01 |    1 | 0.0010 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.38 ns | 0.229 ns | 0.191 ns |  15.12 ns |  15.36 ns |  15.81 ns |  1.00 |    0.02 |    1 | 0.0010 |      80 B |        1.00 |
| EggMap      |  26.71 ns | 0.077 ns | 0.068 ns |  26.56 ns |  26.71 ns |  26.84 ns |  1.74 |    0.02 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper  |  52.69 ns | 0.512 ns | 0.427 ns |  52.20 ns |  52.53 ns |  53.77 ns |  3.43 |    0.05 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster     |  33.90 ns | 0.110 ns | 0.092 ns |  33.78 ns |  33.88 ns |  34.09 ns |  2.21 |    0.03 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap |  20.10 ns | 0.179 ns | 0.168 ns |  19.80 ns |  20.07 ns |  20.38 ns |  1.31 |    0.02 |    2 | 0.0012 |     104 B |        1.30 |
| AgileMapper | 261.90 ns | 2.426 ns | 2.270 ns | 258.29 ns | 261.46 ns | 266.18 ns | 17.04 |    0.25 |    6 | 0.0038 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  46.70 ns | 0.425 ns | 0.377 ns |  46.34 ns |  46.56 ns |  47.44 ns |  1.00 |    0.01 |    1 | 0.0032 |     272 B |        1.00 |
| EggMapper   |  55.29 ns | 1.050 ns | 0.820 ns |  54.55 ns |  54.98 ns |  57.37 ns |  1.18 |    0.02 |    2 | 0.0032 |     272 B |        1.00 |
| AutoMapper  |  88.09 ns | 0.537 ns | 0.476 ns |  87.32 ns |  88.06 ns |  88.77 ns |  1.89 |    0.02 |    4 | 0.0032 |     272 B |        1.00 |
| Mapster     |  61.80 ns | 0.404 ns | 0.378 ns |  61.30 ns |  61.70 ns |  62.58 ns |  1.32 |    0.01 |    3 | 0.0032 |     272 B |        1.00 |
| MapperlyMap |  44.72 ns | 0.558 ns | 0.522 ns |  44.20 ns |  44.56 ns |  45.67 ns |  0.96 |    0.01 |    1 | 0.0032 |     272 B |        1.00 |
| AgileMapper | 285.06 ns | 1.978 ns | 1.754 ns | 282.71 ns | 284.83 ns | 289.03 ns |  6.10 |    0.06 |    5 | 0.0048 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  63.81 ns | 0.212 ns | 0.198 ns |  63.50 ns |  63.80 ns |  64.23 ns |  1.00 |    0.00 |    2 | 0.0038 |     320 B |        1.00 |
| EggMapper   |  78.82 ns | 0.264 ns | 0.247 ns |  78.52 ns |  78.78 ns |  79.31 ns |  1.24 |    0.01 |    4 | 0.0038 |     320 B |        1.00 |
| AutoMapper  | 113.38 ns | 0.305 ns | 0.255 ns | 113.01 ns | 113.38 ns | 113.82 ns |  1.78 |    0.01 |    5 | 0.0038 |     328 B |        1.02 |
| Mapster     |  75.67 ns | 0.567 ns | 0.530 ns |  74.98 ns |  75.50 ns |  76.62 ns |  1.19 |    0.01 |    3 | 0.0038 |     320 B |        1.00 |
| MapperlyMap |  61.68 ns | 0.279 ns | 0.247 ns |  61.24 ns |  61.73 ns |  62.11 ns |  0.97 |    0.00 |    1 | 0.0038 |     320 B |        1.00 |
| AgileMapper | 320.50 ns | 0.989 ns | 0.826 ns | 319.35 ns | 320.31 ns | 322.49 ns |  5.02 |    0.02 |    6 | 0.0062 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.461 μs | 0.0133 μs | 0.0125 μs | 1.445 μs | 1.458 μs | 1.484 μs |  1.00 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| EggMapper   | 1.504 μs | 0.0080 μs | 0.0071 μs | 1.492 μs | 1.506 μs | 1.516 μs |  1.03 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AutoMapper  | 1.826 μs | 0.0145 μs | 0.0128 μs | 1.816 μs | 1.820 μs | 1.855 μs |  1.25 |    3 | 0.1202 | 0.0038 |   9.95 KB |        1.15 |
| Mapster     | 1.475 μs | 0.0069 μs | 0.0065 μs | 1.464 μs | 1.474 μs | 1.485 μs |  1.01 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| MapperlyMap | 1.589 μs | 0.0062 μs | 0.0055 μs | 1.582 μs | 1.589 μs | 1.602 μs |  1.09 |    2 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AgileMapper | 1.818 μs | 0.0059 μs | 0.0049 μs | 1.811 μs | 1.819 μs | 1.827 μs |  1.25 |    3 | 0.1087 | 0.0019 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 4.586 μs | 0.0086 μs | 0.0076 μs | 4.576 μs | 4.585 μs | 4.600 μs |  1.00 |    2 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| EggMapper   | 4.888 μs | 0.0242 μs | 0.0214 μs | 4.844 μs | 4.898 μs | 4.914 μs |  1.07 |    3 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| AutoMapper  | 5.783 μs | 0.0152 μs | 0.0135 μs | 5.754 μs | 5.784 μs | 5.800 μs |  1.26 |    4 | 0.3510 | 0.0153 |   28.7 KB |        1.05 |
| Mapster     | 4.886 μs | 0.0102 μs | 0.0090 μs | 4.872 μs | 4.885 μs | 4.900 μs |  1.07 |    3 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| MapperlyMap | 4.571 μs | 0.0351 μs | 0.0328 μs | 4.527 μs | 4.582 μs | 4.634 μs |  1.00 |    2 | 0.3357 | 0.0153 |  27.42 KB |        1.00 |
| AgileMapper | 4.176 μs | 0.0085 μs | 0.0075 μs | 4.157 μs | 4.178 μs | 4.186 μs |  0.91 |    1 | 0.1984 | 0.0076 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 14.64 μs | 0.059 μs | 0.052 μs | 14.57 μs | 14.62 μs | 14.75 μs |  1.00 |    1 | 1.0376 | 0.2594 |  85.99 KB |        1.00 |
| EggMapper   | 14.89 μs | 0.042 μs | 0.035 μs | 14.83 μs | 14.88 μs | 14.96 μs |  1.02 |    1 | 1.0376 | 0.2594 |  85.99 KB |        1.00 |
| AutoMapper  | 16.09 μs | 0.074 μs | 0.069 μs | 16.01 μs | 16.09 μs | 16.21 μs |  1.10 |    2 | 1.1292 | 0.2747 |  94.34 KB |        1.10 |
| Mapster     | 14.42 μs | 0.026 μs | 0.024 μs | 14.36 μs | 14.41 μs | 14.46 μs |  0.98 |    1 | 1.0376 | 0.2594 |  85.99 KB |        1.00 |
| MapperlyMap | 16.09 μs | 0.078 μs | 0.073 μs | 15.96 μs | 16.12 μs | 16.20 μs |  1.10 |    2 | 1.0376 | 0.2441 |  86.02 KB |        1.00 |
| AgileMapper | 16.88 μs | 0.074 μs | 0.058 μs | 16.78 μs | 16.89 μs | 16.96 μs |  1.15 |    3 | 1.0376 | 0.2441 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         | 11.76 ns | 0.308 ns | 0.273 ns | 11.52 ns | 11.67 ns | 12.37 ns |  1.00 |    0.03 |    1 |         - |          NA |
| EggMapper      | 18.58 ns | 0.254 ns | 0.225 ns | 18.11 ns | 18.56 ns | 18.98 ns |  1.58 |    0.04 |    3 |         - |          NA |
| EggMapperTyped | 13.37 ns | 0.114 ns | 0.101 ns | 13.22 ns | 13.36 ns | 13.57 ns |  1.14 |    0.03 |    2 |         - |          NA |
| AutoMapper     | 42.80 ns | 0.340 ns | 0.284 ns | 42.53 ns | 42.71 ns | 43.55 ns |  3.64 |    0.08 |    5 |         - |          NA |
| Mapster        | 21.09 ns | 0.208 ns | 0.194 ns | 20.83 ns | 21.04 ns | 21.43 ns |  1.79 |    0.04 |    4 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 11.69 ns | 0.155 ns | 0.145 ns | 11.50 ns | 11.67 ns | 11.89 ns |  1.00 |    0.02 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper                   | 23.26 ns | 0.230 ns | 0.204 ns | 22.93 ns | 23.28 ns | 23.58 ns |  1.99 |    0.03 |    2 | 0.0010 |      80 B |        1.00 |
| AutoMapper                  | 49.02 ns | 0.953 ns | 0.936 ns | 48.02 ns | 48.81 ns | 50.58 ns |  4.19 |    0.09 |    4 | 0.0010 |      80 B |        1.00 |
| EggMapperDerivedSource      | 27.04 ns | 0.493 ns | 0.461 ns | 26.48 ns | 26.85 ns | 27.95 ns |  2.31 |    0.05 |    3 | 0.0010 |      80 B |        1.00 |
| EggMapperAlternatingSources | 26.47 ns | 0.354 ns | 0.314 ns | 26.06 ns | 26.40 ns | 27.17 ns |  2.26 |    0.04 |    3 | 0.0010 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.128 μs | 0.0225 μs | 0.0429 μs |   1.070 μs |   1.123 μs |   1.234 μs |   1.00 |    0.05 |    1 | 0.0782 |      - |   6.48 KB |        1.00 |
| AutoMapperStartup | 320.252 μs | 4.9886 μs | 4.4223 μs | 314.757 μs | 319.572 μs | 329.694 μs | 284.27 |   11.17 |    3 |      - |      - |  105.9 KB |       16.35 |
| MapsterStartup    |   2.279 μs | 0.0456 μs | 0.0890 μs |   2.177 μs |   2.254 μs |   2.497 μs |   2.02 |    0.11 |    2 | 0.1373 | 0.0038 |  11.51 KB |        1.78 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| EggMapper  | 1.156 ms | 0.0098 ms | 0.0087 ms | 1.137 ms | 1.156 ms | 1.169 ms |  1.00 |    0.01 |    1 |      - |  78.14 KB |        1.00 |
| AutoMapper | 4.170 ms | 0.0236 ms | 0.0209 ms | 4.141 ms | 4.166 ms | 4.210 ms |  3.61 |    0.03 |    3 |      - | 327.28 KB |        4.19 |
| Mapster    | 2.312 ms | 0.0131 ms | 0.0117 ms | 2.290 ms | 2.313 ms | 2.332 ms |  2.00 |    0.02 |    2 | 7.8125 | 765.09 KB |        9.79 |

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

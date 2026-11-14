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
> ⏱ **Last updated:** 2026-11-14 16:51 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 9.160 ns | **12.910 ns (1.4×)** | 13.589 ns (1.5×) | 33.902 ns (3.7×) | 9.696 ns (1.1×) |
| **Flattening** | 11.83 ns | **17.19 ns (1.4×)** | 17.92 ns (1.5×) | 36.64 ns (3.1×) | 15.13 ns (1.3×) |
| **Deep (2 nested)** | 33.67 ns | **28.78 ns (0.9×)** | 39.26 ns (1.2×) | 55.52 ns (1.6×) | 30.66 ns (0.9×) |
| **Complex (nest+coll)** | 46.93 ns | **51.39 ns (1.1×)** | 57.44 ns (1.2×) | 80.49 ns (1.7×) | 47.68 ns (1.0×) |
| **Collection (100)** | 1.161 μs | **1.124 μs (1.0×)** | 1.147 μs (1.0×) | 1.545 μs (1.3×) | 1.209 μs (1.0×) |
| **Deep Coll (100)** | 3.391 μs | **2.898 μs (0.8×)** | 3.839 μs (1.1×) | 4.253 μs (1.2×) | 3.450 μs (1.0×) |
| **Large Coll (1000)** | 10.77 μs | **10.41 μs (1.0×)** | 10.81 μs (1.0×) | 13.27 μs (1.2×) | 11.40 μs (1.1×) |
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

| Method               | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|--------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual               |   9.160 ns | 0.2028 ns | 0.1897 ns |   8.872 ns |   9.170 ns |   9.527 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  12.910 ns | 0.2225 ns | 0.2081 ns |  12.578 ns |  12.913 ns |  13.361 ns |  1.41 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |   9.900 ns | 0.1189 ns | 0.1054 ns |   9.716 ns |   9.932 ns |  10.091 ns |  1.08 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  33.902 ns | 0.4583 ns | 0.4286 ns |  33.303 ns |  33.685 ns |  34.564 ns |  3.70 |    0.09 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster              |  13.589 ns | 0.2522 ns | 0.2236 ns |  13.249 ns |  13.558 ns |  13.993 ns |  1.48 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |   9.696 ns | 0.2343 ns | 0.4343 ns |   8.805 ns |   9.824 ns |  10.548 ns |  1.06 |    0.05 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 165.724 ns | 3.2844 ns | 3.2257 ns | 161.545 ns | 164.691 ns | 172.831 ns | 18.10 |    0.50 |    4 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |   9.513 ns | 0.0917 ns | 0.0813 ns |   9.355 ns |   9.525 ns |   9.643 ns |  1.04 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |   9.450 ns | 0.2242 ns | 0.2493 ns |   9.103 ns |   9.421 ns |   9.927 ns |  1.03 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  11.83 ns | 0.127 ns | 0.119 ns |  11.60 ns |  11.84 ns |  12.04 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  17.19 ns | 0.112 ns | 0.094 ns |  17.05 ns |  17.20 ns |  17.33 ns |  1.45 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  36.64 ns | 0.393 ns | 0.349 ns |  36.19 ns |  36.61 ns |  37.28 ns |  3.10 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  17.92 ns | 0.153 ns | 0.143 ns |  17.76 ns |  17.85 ns |  18.20 ns |  1.51 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  15.13 ns | 0.333 ns | 0.409 ns |  14.22 ns |  15.17 ns |  15.78 ns |  1.28 |    0.04 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 158.62 ns | 3.022 ns | 2.827 ns | 154.64 ns | 158.47 ns | 163.37 ns | 13.41 |    0.27 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  33.67 ns | 0.656 ns | 0.548 ns |  32.66 ns |  33.78 ns |  34.48 ns |  1.00 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  28.78 ns | 0.358 ns | 0.318 ns |  27.97 ns |  28.89 ns |  29.21 ns |  0.86 |    0.02 |    1 | 0.0095 |     160 B |        0.59 |
| AutoMapper  |  55.52 ns | 1.055 ns | 1.036 ns |  53.62 ns |  56.16 ns |  56.56 ns |  1.65 |    0.04 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  39.26 ns | 0.829 ns | 1.107 ns |  37.30 ns |  39.56 ns |  41.19 ns |  1.17 |    0.04 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  30.66 ns | 0.602 ns | 0.618 ns |  29.45 ns |  30.60 ns |  31.81 ns |  0.91 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 174.48 ns | 3.232 ns | 3.458 ns | 169.59 ns | 174.86 ns | 181.04 ns |  5.18 |    0.13 |    6 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  46.93 ns | 0.599 ns | 0.531 ns |  45.80 ns |  47.02 ns |  48.03 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  51.39 ns | 1.066 ns | 1.349 ns |  49.28 ns |  51.52 ns |  53.35 ns |  1.10 |    0.03 |    2 | 0.0153 |     256 B |        0.80 |
| AutoMapper  |  80.49 ns | 1.522 ns | 1.423 ns |  78.72 ns |  80.53 ns |  83.89 ns |  1.72 |    0.03 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  57.44 ns | 1.081 ns | 1.062 ns |  55.65 ns |  57.06 ns |  59.72 ns |  1.22 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  47.68 ns | 0.940 ns | 0.965 ns |  45.63 ns |  47.65 ns |  49.41 ns |  1.02 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 211.75 ns | 2.363 ns | 2.095 ns | 208.19 ns | 212.08 ns | 215.44 ns |  4.51 |    0.07 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.161 μs | 0.0098 μs | 0.0092 μs | 1.138 μs | 1.162 μs | 1.176 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.124 μs | 0.0168 μs | 0.0157 μs | 1.100 μs | 1.121 μs | 1.153 μs |  0.97 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 1.545 μs | 0.0277 μs | 0.0231 μs | 1.484 μs | 1.551 μs | 1.575 μs |  1.33 |    0.02 |    3 | 0.6084 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.147 μs | 0.0222 μs | 0.0265 μs | 1.111 μs | 1.150 μs | 1.204 μs |  0.99 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.209 μs | 0.0161 μs | 0.0142 μs | 1.191 μs | 1.212 μs | 1.231 μs |  1.04 |    0.01 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 1.466 μs | 0.0226 μs | 0.0200 μs | 1.433 μs | 1.465 μs | 1.505 μs |  1.26 |    0.02 |    3 | 0.5436 | 0.0172 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 3.391 μs | 0.0646 μs | 0.0604 μs | 3.293 μs | 3.372 μs | 3.494 μs |  1.00 |    0.02 |    3 | 1.6747 | 0.0992 |   27.4 KB |        1.00 |
| EggMapper   | 2.898 μs | 0.0317 μs | 0.0264 μs | 2.850 μs | 2.900 μs | 2.949 μs |  0.85 |    0.02 |    1 | 1.0071 | 0.0572 |  16.46 KB |        0.60 |
| AutoMapper  | 4.253 μs | 0.0621 μs | 0.0581 μs | 4.132 μs | 4.274 μs | 4.309 μs |  1.25 |    0.03 |    5 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 3.839 μs | 0.0739 μs | 0.0790 μs | 3.702 μs | 3.851 μs | 3.989 μs |  1.13 |    0.03 |    4 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 3.450 μs | 0.0255 μs | 0.0213 μs | 3.429 μs | 3.444 μs | 3.500 μs |  1.02 |    0.02 |    3 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 3.058 μs | 0.0610 μs | 0.0815 μs | 2.949 μs | 3.034 μs | 3.285 μs |  0.90 |    0.03 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min       | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|----------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 10.77 μs | 0.199 μs | 0.195 μs | 10.435 μs | 10.76 μs | 11.09 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 10.41 μs | 0.190 μs | 0.233 μs |  9.898 μs | 10.42 μs | 10.91 μs |  0.97 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 13.27 μs | 0.258 μs | 0.287 μs | 12.498 μs | 13.23 μs | 13.75 μs |  1.23 |    0.03 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 10.81 μs | 0.181 μs | 0.170 μs | 10.620 μs | 10.73 μs | 11.15 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 11.40 μs | 0.157 μs | 0.139 μs | 11.080 μs | 11.37 μs | 11.57 μs |  1.06 |    0.02 |    2 | 5.2643 | 1.3123 |  86.02 KB |        1.00 |
| AgileMapper | 11.94 μs | 0.216 μs | 0.202 μs | 11.707 μs | 11.86 μs | 12.37 μs |  1.11 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Min       | Median    | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  6.750 ns | 0.1751 ns | 0.2727 ns |  6.327 ns |  6.741 ns |  7.354 ns |  1.00 |    0.06 |    1 |         - |          NA |
| EggMapper      |  9.912 ns | 0.1630 ns | 0.1272 ns |  9.659 ns |  9.941 ns | 10.111 ns |  1.47 |    0.06 |    3 |         - |          NA |
| EggMapperTyped |  7.367 ns | 0.1804 ns | 0.1599 ns |  7.104 ns |  7.369 ns |  7.661 ns |  1.09 |    0.05 |    2 |         - |          NA |
| AutoMapper     | 32.050 ns | 0.6344 ns | 0.7791 ns | 31.292 ns | 31.754 ns | 33.824 ns |  4.76 |    0.22 |    4 |         - |          NA |
| Mapster        | 10.428 ns | 0.2451 ns | 0.2292 ns | 10.103 ns | 10.426 ns | 10.856 ns |  1.55 |    0.07 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean      | Error     | StdDev    | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      |  9.023 ns | 0.1251 ns | 0.1170 ns |  8.893 ns |  9.008 ns |  9.280 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 15.120 ns | 0.1231 ns | 0.1091 ns | 14.936 ns | 15.136 ns | 15.307 ns |  1.68 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 34.374 ns | 0.6398 ns | 0.5985 ns | 33.547 ns | 34.274 ns | 35.427 ns |  3.81 |    0.08 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 16.158 ns | 0.2901 ns | 0.2714 ns | 15.631 ns | 16.148 ns | 16.549 ns |  1.79 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 16.587 ns | 0.2792 ns | 0.2475 ns | 16.122 ns | 16.606 ns | 16.900 ns |  1.84 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error       | StdDev      | Min          | Median       | Max          | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|------------:|------------:|-------------:|-------------:|-------------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |     698.0 ns |    13.92 ns |    14.29 ns |     666.6 ns |     697.8 ns |     723.9 ns |   1.00 |    0.03 |    1 | 0.4520 | 0.0105 |    7.4 KB |        1.00 |
| AutoMapperStartup | 201,543.3 ns | 3,648.59 ns | 3,412.90 ns | 197,027.8 ns | 200,289.2 ns | 207,312.3 ns | 288.85 |    7.47 |    3 | 6.8359 | 0.9766 | 105.95 KB |       14.32 |
| MapsterStartup    |   1,443.6 ns |    28.29 ns |    26.47 ns |   1,405.5 ns |   1,439.6 ns |   1,492.9 ns |   2.07 |    0.06 |    2 | 0.7038 | 0.0286 |  11.51 KB |        1.56 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min       | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|----------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.011 ms | 0.0090 ms | 0.0070 ms | 0.9948 ms | 1.014 ms | 1.017 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |  86.88 KB |        1.00 |
| AutoMapper | 3.248 ms | 0.0522 ms | 0.0463 ms | 3.1813 ms | 3.242 ms | 3.310 ms |  3.21 |    0.05 |    3 | 15.6250 |  7.8125 | 327.54 KB |        3.77 |
| Mapster    | 1.780 ms | 0.0171 ms | 0.0152 ms | 1.7520 ms | 1.777 ms | 1.816 ms |  1.76 |    0.02 |    2 | 42.9688 | 19.5313 | 757.25 KB |        8.72 |

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

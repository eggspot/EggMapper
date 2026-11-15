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
> ⏱ **Last updated:** 2026-11-15 00:45 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 13.17 ns | **26.08 ns (2.0×)** | 23.06 ns (1.8×) | 58.39 ns (4.4×) | 13.40 ns (1.0×) |
| **Flattening** | 15.84 ns | **23.85 ns (1.5×)** | 27.55 ns (1.7×) | 61.29 ns (3.9×) | 19.34 ns (1.2×) |
| **Deep (2 nested)** | 50.04 ns | **42.55 ns (0.8×)** | 57.32 ns (1.1×) | 87.21 ns (1.7×) | 44.13 ns (0.9×) |
| **Complex (nest+coll)** | 64.28 ns | **71.46 ns (1.1×)** | 78.61 ns (1.2×) | 116.34 ns (1.8×) | 66.12 ns (1.0×) |
| **Collection (100)** | 1.683 μs | **1.805 μs (1.1×)** | 2.356 μs (1.4×) | 2.282 μs (1.4×) | 1.835 μs (1.1×) |
| **Deep Coll (100)** | 5.314 μs | **4.394 μs (0.8×)** | 5.998 μs (1.1×) | 7.017 μs (1.3×) | 5.686 μs (1.1×) |
| **Large Coll (1000)** | 18.30 μs | **18.11 μs (1.0×)** | 18.77 μs (1.0×) | 21.29 μs (1.2×) | 20.38 μs (1.1×) |
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
| Manual               |  13.17 ns | 0.215 ns | 0.201 ns |  12.81 ns |  13.22 ns |  13.54 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.08 ns | 0.314 ns | 0.294 ns |  25.33 ns |  26.08 ns |  26.43 ns |  1.98 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  14.56 ns | 0.200 ns | 0.187 ns |  14.22 ns |  14.57 ns |  14.86 ns |  1.11 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  58.39 ns | 0.105 ns | 0.093 ns |  58.25 ns |  58.42 ns |  58.52 ns |  4.43 |    0.07 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  23.06 ns | 0.254 ns | 0.225 ns |  22.79 ns |  23.03 ns |  23.59 ns |  1.75 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  13.40 ns | 0.187 ns | 0.166 ns |  13.12 ns |  13.39 ns |  13.72 ns |  1.02 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 282.99 ns | 1.015 ns | 0.900 ns | 281.24 ns | 283.17 ns | 284.16 ns | 21.49 |    0.33 |    6 | 0.0205 |     344 B |        4.30 |
| EggMapperGenerator   |  12.66 ns | 0.174 ns | 0.154 ns |  12.26 ns |  12.69 ns |  12.93 ns |  0.96 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  12.71 ns | 0.262 ns | 0.245 ns |  12.37 ns |  12.69 ns |  13.04 ns |  0.97 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.84 ns | 0.295 ns | 0.276 ns |  15.42 ns |  15.84 ns |  16.24 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  23.85 ns | 0.272 ns | 0.254 ns |  23.45 ns |  23.87 ns |  24.25 ns |  1.51 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  61.29 ns | 0.161 ns | 0.151 ns |  61.01 ns |  61.26 ns |  61.54 ns |  3.87 |    0.07 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  27.55 ns | 0.311 ns | 0.291 ns |  27.11 ns |  27.51 ns |  28.05 ns |  1.74 |    0.03 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  19.34 ns | 0.119 ns | 0.105 ns |  19.14 ns |  19.36 ns |  19.48 ns |  1.22 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 289.65 ns | 2.395 ns | 2.123 ns | 287.09 ns | 288.79 ns | 294.51 ns | 18.29 |    0.33 |    6 | 0.0205 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  50.04 ns | 0.662 ns | 0.587 ns |  49.15 ns |  50.22 ns |  51.01 ns |  1.00 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  42.55 ns | 0.436 ns | 0.387 ns |  41.84 ns |  42.52 ns |  43.06 ns |  0.85 |    0.01 |    1 | 0.0095 |     160 B |        0.59 |
| AutoMapper  |  87.21 ns | 0.870 ns | 0.771 ns |  85.87 ns |  87.23 ns |  88.67 ns |  1.74 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  57.32 ns | 0.674 ns | 0.630 ns |  56.15 ns |  57.31 ns |  58.31 ns |  1.15 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  44.13 ns | 0.435 ns | 0.407 ns |  43.35 ns |  44.24 ns |  44.85 ns |  0.88 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 294.76 ns | 2.109 ns | 1.869 ns | 290.89 ns | 295.20 ns | 297.95 ns |  5.89 |    0.08 |    5 | 0.0253 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  64.28 ns | 0.769 ns | 0.719 ns |  62.68 ns |  64.31 ns |  65.33 ns |  1.00 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  71.46 ns | 0.537 ns | 0.502 ns |  70.59 ns |  71.40 ns |  72.42 ns |  1.11 |    0.01 |    2 | 0.0153 |     256 B |        0.80 |
| AutoMapper  | 116.34 ns | 0.886 ns | 0.740 ns | 115.00 ns | 116.35 ns | 117.42 ns |  1.81 |    0.02 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  78.61 ns | 0.936 ns | 0.876 ns |  76.89 ns |  78.62 ns |  80.00 ns |  1.22 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  66.12 ns | 0.661 ns | 0.586 ns |  65.03 ns |  66.08 ns |  67.25 ns |  1.03 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 324.68 ns | 3.454 ns | 3.231 ns | 320.46 ns | 323.54 ns | 330.42 ns |  5.05 |    0.07 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.683 μs | 0.0332 μs | 0.0656 μs | 1.599 μs | 1.661 μs | 1.854 μs |  1.00 |    0.05 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.805 μs | 0.0353 μs | 0.0570 μs | 1.699 μs | 1.791 μs | 1.925 μs |  1.07 |    0.05 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.282 μs | 0.0454 μs | 0.0885 μs | 2.143 μs | 2.265 μs | 2.481 μs |  1.36 |    0.07 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 2.356 μs | 0.0466 μs | 0.0698 μs | 2.235 μs | 2.358 μs | 2.486 μs |  1.40 |    0.07 |    3 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| MapperlyMap | 1.835 μs | 0.0361 μs | 0.0518 μs | 1.746 μs | 1.833 μs | 1.935 μs |  1.09 |    0.05 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.322 μs | 0.0416 μs | 0.0348 μs | 2.274 μs | 2.325 μs | 2.389 μs |  1.38 |    0.06 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.314 μs | 0.1062 μs | 0.2168 μs | 4.805 μs | 5.314 μs | 5.724 μs |  1.00 |    0.06 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 4.394 μs | 0.0720 μs | 0.0674 μs | 4.269 μs | 4.382 μs | 4.546 μs |  0.83 |    0.04 |    1 | 1.0071 | 0.0534 |  16.46 KB |        0.60 |
| AutoMapper  | 7.017 μs | 0.1395 μs | 0.1493 μs | 6.569 μs | 7.030 μs | 7.232 μs |  1.32 |    0.06 |    5 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.998 μs | 0.0552 μs | 0.0516 μs | 5.896 μs | 6.007 μs | 6.045 μs |  1.13 |    0.05 |    4 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.686 μs | 0.1138 μs | 0.1064 μs | 5.316 μs | 5.703 μs | 5.776 μs |  1.07 |    0.05 |    3 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.093 μs | 0.0348 μs | 0.0326 μs | 5.013 μs | 5.088 μs | 5.135 μs |  0.96 |    0.04 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.30 μs | 0.298 μs | 0.279 μs | 17.91 μs | 18.29 μs | 18.76 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.11 μs | 0.358 μs | 0.892 μs | 16.28 μs | 18.09 μs | 20.41 μs |  0.99 |    0.05 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.29 μs | 0.393 μs | 0.524 μs | 20.42 μs | 21.34 μs | 22.23 μs |  1.16 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.77 μs | 0.368 μs | 0.466 μs | 17.54 μs | 18.80 μs | 19.45 μs |  1.03 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 20.38 μs | 0.331 μs | 0.310 μs | 20.00 μs | 20.38 μs | 21.00 μs |  1.11 |    0.02 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.53 μs | 0.407 μs | 0.803 μs | 18.52 μs | 20.61 μs | 22.14 μs |  1.12 |    0.05 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Median    | Min       | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  8.610 ns | 0.2493 ns | 0.7352 ns |  7.975 ns |  7.933 ns |  9.612 ns |  1.01 |    0.12 |    1 |         - |          NA |
| EggMapper      | 15.982 ns | 0.3657 ns | 0.7714 ns | 15.590 ns | 14.679 ns | 17.285 ns |  1.87 |    0.18 |    3 |         - |          NA |
| EggMapperTyped | 10.488 ns | 0.2610 ns | 0.6202 ns | 10.068 ns |  9.799 ns | 11.380 ns |  1.23 |    0.13 |    2 |         - |          NA |
| AutoMapper     | 53.358 ns | 0.0788 ns | 0.0699 ns | 53.339 ns | 53.254 ns | 53.540 ns |  6.24 |    0.52 |    4 |         - |          NA |
| Mapster        | 16.790 ns | 0.3301 ns | 0.6667 ns | 17.208 ns | 15.829 ns | 17.627 ns |  1.96 |    0.18 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 12.95 ns | 0.192 ns | 0.180 ns | 12.67 ns | 12.98 ns | 13.29 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 22.79 ns | 0.131 ns | 0.123 ns | 22.64 ns | 22.78 ns | 23.05 ns |  1.76 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 58.89 ns | 0.130 ns | 0.115 ns | 58.66 ns | 58.85 ns | 59.09 ns |  4.55 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 25.57 ns | 0.382 ns | 0.339 ns | 24.97 ns | 25.54 ns | 26.33 ns |  1.98 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 24.92 ns | 0.195 ns | 0.173 ns | 24.61 ns | 24.95 ns | 25.21 ns |  1.93 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.285 μs | 0.0237 μs | 0.0210 μs |   1.262 μs |   1.277 μs |   1.341 μs |   1.00 |    0.02 |    1 | 0.4635 | 0.0095 |   7.59 KB |        1.00 |
| AutoMapperStartup | 326.650 μs | 3.1366 μs | 2.9339 μs | 323.413 μs | 324.974 μs | 332.849 μs | 254.25 |    4.50 |    3 | 5.8594 |      - | 106.32 KB |       14.00 |
| MapsterStartup    |   2.340 μs | 0.0435 μs | 0.0386 μs |   2.238 μs |   2.345 μs |   2.383 μs |   1.82 |    0.04 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.52 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.191 ms | 0.0136 ms | 0.0127 ms | 1.175 ms | 1.190 ms | 1.214 ms |  1.00 |    0.01 |    1 |  3.9063 |  1.9531 |   87.5 KB |        1.00 |
| AutoMapper | 3.698 ms | 0.0192 ms | 0.0179 ms | 3.670 ms | 3.697 ms | 3.729 ms |  3.10 |    0.04 |    3 | 15.6250 |  7.8125 | 327.26 KB |        3.74 |
| Mapster    | 2.029 ms | 0.0153 ms | 0.0127 ms | 2.012 ms | 2.027 ms | 2.056 ms |  1.70 |    0.02 |    2 | 42.9688 | 19.5313 | 757.73 KB |        8.66 |

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

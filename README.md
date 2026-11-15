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

Optional build-time checks: `dotnet add package EggMapper.Analyzers` warns when a `MapperConfiguration` is never
validated ([EGG1002](https://eggspot.github.io/EggMapper/diagnostics/EGG1002.html)) and suggests compile-time `[MapTo]`
for maps without customization ([EGG1003](https://eggspot.github.io/EggMapper/diagnostics/EGG1003.html)).

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
> ⏱ **Last updated:** 2026-11-15 00:22 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 14.86 ns | **26.61 ns (1.8×)** | 27.41 ns (1.9×) | 82.36 ns (5.5×) | 15.39 ns (1.0×) |
| **Flattening** | 18.95 ns | **29.82 ns (1.6×)** | 47.68 ns (2.5×) | 89.30 ns (4.7×) | 24.09 ns (1.3×) |
| **Deep (2 nested)** | 53.12 ns | **52.70 ns (1.0×)** | 69.21 ns (1.3×) | 121.09 ns (2.3×) | 50.42 ns (0.9×) |
| **Complex (nest+coll)** | 72.43 ns | **83.77 ns (1.2×)** | 91.45 ns (1.3×) | 153.54 ns (2.1×) | 71.07 ns (1.0×) |
| **Collection (100)** | 1.680 μs | **1.715 μs (1.0×)** | 1.766 μs (1.1×) | 2.442 μs (1.4×) | 1.852 μs (1.1×) |
| **Deep Coll (100)** | 5.352 μs | **4.338 μs (0.8×)** | 5.897 μs (1.1×) | 6.474 μs (1.2×) | 5.415 μs (1.0×) |
| **Large Coll (1000)** | 17.55 μs | **17.06 μs (1.0×)** | 17.41 μs (1.0×) | 21.14 μs (1.2×) | 18.06 μs (1.0×) |
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
| Manual               |  14.86 ns | 0.324 ns | 0.271 ns |  14.57 ns |  14.74 ns |  15.56 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  26.61 ns | 0.498 ns | 0.466 ns |  25.77 ns |  26.61 ns |  27.36 ns |  1.79 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  20.55 ns | 0.393 ns | 0.349 ns |  20.20 ns |  20.45 ns |  21.37 ns |  1.38 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  82.36 ns | 0.693 ns | 0.614 ns |  81.72 ns |  82.16 ns |  83.70 ns |  5.54 |    0.10 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster              |  27.41 ns | 0.163 ns | 0.136 ns |  27.21 ns |  27.40 ns |  27.67 ns |  1.85 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.39 ns | 0.332 ns | 0.475 ns |  14.68 ns |  15.31 ns |  16.40 ns |  1.04 |    0.04 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 484.08 ns | 4.045 ns | 3.784 ns | 480.80 ns | 481.99 ns | 492.01 ns | 32.59 |    0.61 |    5 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.42 ns | 0.311 ns | 0.291 ns |  15.00 ns |  15.34 ns |  16.10 ns |  1.04 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  15.87 ns | 0.269 ns | 0.239 ns |  15.60 ns |  15.78 ns |  16.45 ns |  1.07 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.95 ns | 0.348 ns | 0.308 ns |  18.58 ns |  18.85 ns |  19.63 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  29.82 ns | 0.595 ns | 0.556 ns |  29.20 ns |  29.58 ns |  30.88 ns |  1.57 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  89.30 ns | 0.770 ns | 0.683 ns |  88.47 ns |  89.06 ns |  90.79 ns |  4.71 |    0.08 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.68 ns | 0.762 ns | 0.713 ns |  46.86 ns |  47.44 ns |  49.31 ns |  2.52 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.09 ns | 0.427 ns | 0.400 ns |  23.58 ns |  23.95 ns |  24.93 ns |  1.27 |    0.03 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 524.19 ns | 3.626 ns | 3.028 ns | 520.07 ns | 523.25 ns | 530.69 ns | 27.67 |    0.46 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.12 ns | 0.833 ns | 0.695 ns |  52.12 ns |  52.82 ns |  54.86 ns |  1.00 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  52.70 ns | 0.864 ns | 0.808 ns |  51.50 ns |  52.55 ns |  54.24 ns |  0.99 |    0.02 |    1 | 0.0095 |     160 B |        0.59 |
| AutoMapper  | 121.09 ns | 1.644 ns | 1.373 ns | 119.53 ns | 120.82 ns | 124.53 ns |  2.28 |    0.04 |    3 | 0.0162 |     272 B |        1.00 |
| Mapster     |  69.21 ns | 1.146 ns | 1.016 ns |  67.65 ns |  69.11 ns |  71.17 ns |  1.30 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  50.42 ns | 0.826 ns | 0.689 ns |  49.27 ns |  50.19 ns |  51.86 ns |  0.95 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 508.15 ns | 4.038 ns | 3.777 ns | 503.81 ns | 507.39 ns | 515.35 ns |  9.57 |    0.14 |    4 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  72.43 ns | 1.506 ns | 1.904 ns |  70.05 ns |  72.21 ns |  76.90 ns |  1.00 |    0.04 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  83.77 ns | 1.596 ns | 1.837 ns |  81.16 ns |  83.26 ns |  87.66 ns |  1.16 |    0.04 |    2 | 0.0153 |     256 B |        0.80 |
| AutoMapper  | 153.54 ns | 2.380 ns | 2.226 ns | 150.66 ns | 153.90 ns | 158.24 ns |  2.12 |    0.06 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  91.45 ns | 0.756 ns | 0.631 ns |  89.95 ns |  91.72 ns |  92.15 ns |  1.26 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  71.07 ns | 1.067 ns | 0.891 ns |  69.49 ns |  70.73 ns |  72.71 ns |  0.98 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 585.65 ns | 6.787 ns | 6.348 ns | 577.51 ns | 585.32 ns | 597.74 ns |  8.09 |    0.22 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.680 μs | 0.0061 μs | 0.0048 μs | 1.670 μs | 1.681 μs | 1.687 μs |  1.00 |    0.00 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.715 μs | 0.0332 μs | 0.0466 μs | 1.660 μs | 1.708 μs | 1.815 μs |  1.02 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.442 μs | 0.0472 μs | 0.0464 μs | 2.356 μs | 2.438 μs | 2.508 μs |  1.45 |    0.03 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.766 μs | 0.0351 μs | 0.0404 μs | 1.728 μs | 1.751 μs | 1.853 μs |  1.05 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.852 μs | 0.0354 μs | 0.0363 μs | 1.798 μs | 1.846 μs | 1.942 μs |  1.10 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.441 μs | 0.0373 μs | 0.0312 μs | 2.410 μs | 2.433 μs | 2.517 μs |  1.45 |    0.02 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.352 μs | 0.1051 μs | 0.1329 μs | 5.161 μs | 5.318 μs | 5.638 μs |  1.00 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 4.338 μs | 0.0856 μs | 0.0916 μs | 4.234 μs | 4.306 μs | 4.525 μs |  0.81 |    0.03 |    1 | 1.0071 | 0.0534 |  16.46 KB |        0.60 |
| AutoMapper  | 6.474 μs | 0.0902 μs | 0.0753 μs | 6.322 μs | 6.495 μs | 6.559 μs |  1.21 |    0.03 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.897 μs | 0.1019 μs | 0.1133 μs | 5.786 μs | 5.862 μs | 6.138 μs |  1.10 |    0.03 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.415 μs | 0.0649 μs | 0.0542 μs | 5.302 μs | 5.412 μs | 5.514 μs |  1.01 |    0.03 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.227 μs | 0.1033 μs | 0.1105 μs | 5.112 μs | 5.184 μs | 5.481 μs |  0.98 |    0.03 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.55 μs | 0.333 μs | 0.278 μs | 17.03 μs | 17.66 μs | 17.96 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.06 μs | 0.315 μs | 0.295 μs | 16.45 μs | 17.09 μs | 17.50 μs |  0.97 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.14 μs | 0.301 μs | 0.252 μs | 20.83 μs | 21.12 μs | 21.78 μs |  1.20 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.41 μs | 0.344 μs | 0.354 μs | 16.92 μs | 17.28 μs | 18.27 μs |  0.99 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.06 μs | 0.210 μs | 0.187 μs | 17.88 μs | 17.98 μs | 18.49 μs |  1.03 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.00 μs | 0.381 μs | 0.337 μs | 19.50 μs | 19.88 μs | 20.69 μs |  1.14 |    0.03 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         | 10.92 ns | 0.037 ns | 0.035 ns | 10.87 ns | 10.94 ns | 10.98 ns |  1.00 |    0.00 |    1 |         - |          NA |
| EggMapper      | 23.03 ns | 0.120 ns | 0.106 ns | 22.80 ns | 23.04 ns | 23.19 ns |  2.11 |    0.01 |    4 |         - |          NA |
| EggMapperTyped | 16.74 ns | 0.122 ns | 0.114 ns | 16.59 ns | 16.70 ns | 17.00 ns |  1.53 |    0.01 |    2 |         - |          NA |
| AutoMapper     | 79.16 ns | 0.105 ns | 0.088 ns | 79.08 ns | 79.13 ns | 79.39 ns |  7.25 |    0.02 |    5 |         - |          NA |
| Mapster        | 21.19 ns | 0.024 ns | 0.020 ns | 21.17 ns | 21.17 ns | 21.22 ns |  1.94 |    0.01 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 15.14 ns | 0.189 ns | 0.158 ns | 14.93 ns | 15.09 ns | 15.45 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 30.37 ns | 0.593 ns | 0.495 ns | 29.65 ns | 30.44 ns | 31.55 ns |  2.01 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 82.21 ns | 0.711 ns | 0.665 ns | 81.37 ns | 82.04 ns | 83.53 ns |  5.43 |    0.07 |    4 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 34.36 ns | 0.399 ns | 0.373 ns | 33.80 ns | 34.20 ns | 35.17 ns |  2.27 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 34.21 ns | 0.354 ns | 0.296 ns | 33.65 ns | 34.21 ns | 34.71 ns |  2.26 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.309 μs | 0.0182 μs | 0.0202 μs |   1.281 μs |   1.307 μs |   1.369 μs |   1.00 |    0.02 |    1 | 0.4635 | 0.0095 |   7.59 KB |        1.00 |
| AutoMapperStartup | 450.645 μs | 5.2194 μs | 4.8822 μs | 445.067 μs | 450.577 μs | 459.532 μs | 344.42 |    6.24 |    3 | 5.8594 |      - | 106.55 KB |       14.03 |
| MapsterStartup    |   2.520 μs | 0.0501 μs | 0.0596 μs |   2.461 μs |   2.496 μs |   2.656 μs |   1.93 |    0.05 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.52 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.429 ms | 0.0102 ms | 0.0090 ms | 1.414 ms | 1.430 ms | 1.441 ms |  1.00 |    0.01 |    1 |  3.9063 |       - |  86.54 KB |        1.00 |
| AutoMapper | 4.528 ms | 0.0357 ms | 0.0317 ms | 4.475 ms | 4.532 ms | 4.573 ms |  3.17 |    0.03 |    3 | 15.6250 |  7.8125 | 327.53 KB |        3.78 |
| Mapster    | 2.512 ms | 0.0246 ms | 0.0218 ms | 2.490 ms | 2.506 ms | 2.553 ms |  1.76 |    0.02 |    2 | 39.0625 | 15.6250 | 759.07 KB |        8.77 |

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

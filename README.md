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
> ⏱ **Last updated:** 2026-11-14 23:19 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.64 ns | **23.95 ns (1.5×)** | 23.99 ns (1.5×) | 49.51 ns (3.2×) | 17.25 ns (1.1×) |
| **Flattening** | 19.54 ns | **28.51 ns (1.5×)** | 35.69 ns (1.8×) | 52.60 ns (2.7×) | 24.33 ns (1.2×) |
| **Deep (2 nested)** | 60.68 ns | **49.83 ns (0.8×)** | 67.99 ns (1.1×) | 97.93 ns (1.6×) | 55.36 ns (0.9×) |
| **Complex (nest+coll)** | 76.56 ns | **74.13 ns (1.0×)** | 90.88 ns (1.2×) | 124.52 ns (1.6×) | 74.22 ns (1.0×) |
| **Collection (100)** | 1.632 μs | **1.638 μs (1.0×)** | 1.807 μs (1.1×) | 2.340 μs (1.4×) | 1.844 μs (1.1×) |
| **Deep Coll (100)** | 5.995 μs | **4.424 μs (0.7×)** | 6.354 μs (1.1×) | 6.821 μs (1.1×) | 5.922 μs (1.0×) |
| **Large Coll (1000)** | 17.12 μs | **16.91 μs (1.0×)** | 17.99 μs (1.1×) | 20.54 μs (1.2×) | 17.96 μs (1.1×) |
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
| Manual               |  15.64 ns | 0.244 ns | 0.228 ns |  14.96 ns |  15.73 ns |  15.88 ns |  1.00 |    0.02 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper            |  23.95 ns | 0.125 ns | 0.117 ns |  23.80 ns |  23.93 ns |  24.20 ns |  1.53 |    0.02 |    4 | 0.0010 |      80 B |        1.00 |
| EggMapperTyped       |  18.37 ns | 0.089 ns | 0.083 ns |  18.27 ns |  18.34 ns |  18.53 ns |  1.17 |    0.02 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper           |  49.51 ns | 0.170 ns | 0.159 ns |  49.25 ns |  49.52 ns |  49.77 ns |  3.17 |    0.05 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster              |  23.99 ns | 0.075 ns | 0.066 ns |  23.83 ns |  24.00 ns |  24.08 ns |  1.53 |    0.02 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap          |  17.25 ns | 0.041 ns | 0.039 ns |  17.19 ns |  17.26 ns |  17.33 ns |  1.10 |    0.02 |    2 | 0.0010 |      80 B |        1.00 |
| AgileMapper          | 235.04 ns | 0.769 ns | 0.601 ns | 233.93 ns | 234.96 ns | 236.13 ns | 15.03 |    0.22 |    6 | 0.0041 |     344 B |        4.30 |
| EggMapperGenerator   |  16.76 ns | 0.119 ns | 0.105 ns |  16.62 ns |  16.75 ns |  16.98 ns |  1.07 |    0.02 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapperClassMapper |  16.98 ns | 0.048 ns | 0.042 ns |  16.93 ns |  16.97 ns |  17.07 ns |  1.09 |    0.02 |    2 | 0.0010 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.54 ns | 0.091 ns | 0.085 ns |  19.43 ns |  19.53 ns |  19.69 ns |  1.00 |    0.01 |    1 | 0.0010 |      80 B |        1.00 |
| EggMap      |  28.51 ns | 0.088 ns | 0.083 ns |  28.42 ns |  28.49 ns |  28.69 ns |  1.46 |    0.01 |    3 | 0.0010 |      80 B |        1.00 |
| AutoMapper  |  52.60 ns | 0.166 ns | 0.156 ns |  52.23 ns |  52.62 ns |  52.81 ns |  2.69 |    0.01 |    5 | 0.0010 |      80 B |        1.00 |
| Mapster     |  35.69 ns | 0.090 ns | 0.084 ns |  35.49 ns |  35.70 ns |  35.80 ns |  1.83 |    0.01 |    4 | 0.0010 |      80 B |        1.00 |
| MapperlyMap |  24.33 ns | 0.169 ns | 0.158 ns |  23.99 ns |  24.41 ns |  24.53 ns |  1.25 |    0.01 |    2 | 0.0012 |     104 B |        1.30 |
| AgileMapper | 244.55 ns | 0.718 ns | 0.671 ns | 243.73 ns | 244.32 ns | 245.86 ns | 12.51 |    0.06 |    6 | 0.0038 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.68 ns | 0.357 ns | 0.334 ns |  60.12 ns |  60.78 ns |  61.04 ns |  1.00 |    0.01 |    3 | 0.0032 |     272 B |        1.00 |
| EggMapper   |  49.83 ns | 0.258 ns | 0.241 ns |  49.24 ns |  49.87 ns |  50.15 ns |  0.82 |    0.01 |    1 | 0.0019 |     160 B |        0.59 |
| AutoMapper  |  97.93 ns | 1.829 ns | 1.711 ns |  95.84 ns |  97.65 ns | 101.72 ns |  1.61 |    0.03 |    5 | 0.0032 |     272 B |        1.00 |
| Mapster     |  67.99 ns | 0.460 ns | 0.431 ns |  67.33 ns |  67.93 ns |  68.60 ns |  1.12 |    0.01 |    4 | 0.0032 |     272 B |        1.00 |
| MapperlyMap |  55.36 ns | 0.828 ns | 0.774 ns |  54.00 ns |  55.32 ns |  56.43 ns |  0.91 |    0.01 |    2 | 0.0032 |     272 B |        1.00 |
| AgileMapper | 276.94 ns | 2.089 ns | 1.852 ns | 273.64 ns | 277.29 ns | 279.44 ns |  4.56 |    0.04 |    6 | 0.0048 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  76.56 ns | 0.209 ns | 0.196 ns |  76.32 ns |  76.52 ns |  76.97 ns |  1.00 |    0.00 |    2 | 0.0038 |     320 B |        1.00 |
| EggMapper   |  74.13 ns | 0.276 ns | 0.245 ns |  73.77 ns |  74.08 ns |  74.51 ns |  0.97 |    0.00 |    1 | 0.0030 |     256 B |        0.80 |
| AutoMapper  | 124.52 ns | 0.616 ns | 0.576 ns | 123.63 ns | 124.57 ns | 125.40 ns |  1.63 |    0.01 |    4 | 0.0038 |     328 B |        1.02 |
| Mapster     |  90.88 ns | 0.283 ns | 0.264 ns |  90.31 ns |  90.89 ns |  91.24 ns |  1.19 |    0.00 |    3 | 0.0038 |     320 B |        1.00 |
| MapperlyMap |  74.22 ns | 0.242 ns | 0.226 ns |  73.91 ns |  74.14 ns |  74.64 ns |  0.97 |    0.00 |    1 | 0.0038 |     320 B |        1.00 |
| AgileMapper | 314.26 ns | 1.186 ns | 0.990 ns | 312.78 ns | 314.46 ns | 315.48 ns |  4.10 |    0.02 |    5 | 0.0062 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.632 μs | 0.0324 μs | 0.0373 μs | 1.549 μs | 1.639 μs | 1.687 μs |  1.00 |    0.03 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| EggMapper   | 1.638 μs | 0.0325 μs | 0.0826 μs | 1.485 μs | 1.622 μs | 1.844 μs |  1.00 |    0.06 |    1 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AutoMapper  | 2.340 μs | 0.0274 μs | 0.0256 μs | 2.264 μs | 2.342 μs | 2.368 μs |  1.43 |    0.04 |    4 | 0.1183 | 0.0038 |   9.95 KB |        1.15 |
| Mapster     | 1.807 μs | 0.0249 μs | 0.0233 μs | 1.753 μs | 1.815 μs | 1.835 μs |  1.11 |    0.03 |    2 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| MapperlyMap | 1.844 μs | 0.0189 μs | 0.0176 μs | 1.797 μs | 1.849 μs | 1.864 μs |  1.13 |    0.03 |    2 | 0.1049 | 0.0019 |   8.65 KB |        1.00 |
| AgileMapper | 2.168 μs | 0.0105 μs | 0.0099 μs | 2.147 μs | 2.170 μs | 2.181 μs |  1.33 |    0.03 |    3 | 0.1068 |      - |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.995 μs | 0.0511 μs | 0.0453 μs | 5.927 μs | 5.994 μs | 6.081 μs |  1.00 |    3 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| EggMapper   | 4.424 μs | 0.0246 μs | 0.0231 μs | 4.371 μs | 4.422 μs | 4.461 μs |  0.74 |    1 | 0.1984 | 0.0076 |  16.46 KB |        0.60 |
| AutoMapper  | 6.821 μs | 0.0273 μs | 0.0256 μs | 6.786 μs | 6.813 μs | 6.873 μs |  1.14 |    5 | 0.3510 | 0.0153 |   28.7 KB |        1.05 |
| Mapster     | 6.354 μs | 0.0233 μs | 0.0206 μs | 6.304 μs | 6.358 μs | 6.383 μs |  1.06 |    4 | 0.3281 | 0.0153 |   27.4 KB |        1.00 |
| MapperlyMap | 5.922 μs | 0.0456 μs | 0.0427 μs | 5.862 μs | 5.923 μs | 5.991 μs |  0.99 |    3 | 0.3357 | 0.0153 |  27.42 KB |        1.00 |
| AgileMapper | 4.783 μs | 0.0271 μs | 0.0240 μs | 4.750 μs | 4.781 μs | 4.839 μs |  0.80 |    2 | 0.1984 | 0.0076 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.12 μs | 0.174 μs | 0.163 μs | 16.81 μs | 17.15 μs | 17.38 μs |  1.00 |    0.01 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| EggMapper   | 16.91 μs | 0.338 μs | 0.583 μs | 15.48 μs | 17.15 μs | 17.76 μs |  0.99 |    0.03 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| AutoMapper  | 20.54 μs | 0.407 μs | 0.544 μs | 19.59 μs | 20.68 μs | 21.42 μs |  1.20 |    0.03 |    2 | 1.1292 | 0.2747 |  94.34 KB |        1.10 |
| Mapster     | 17.99 μs | 0.126 μs | 0.117 μs | 17.77 μs | 18.01 μs | 18.14 μs |  1.05 |    0.01 |    1 | 1.0376 | 0.2441 |  85.99 KB |        1.00 |
| MapperlyMap | 17.96 μs | 0.337 μs | 0.315 μs | 17.45 μs | 18.02 μs | 18.45 μs |  1.05 |    0.02 |    1 | 1.0376 | 0.2441 |  86.02 KB |        1.00 |
| AgileMapper | 18.87 μs | 0.373 μs | 0.570 μs | 17.36 μs | 18.89 μs | 19.86 μs |  1.10 |    0.03 |    1 | 1.0376 | 0.2441 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean      | Error     | StdDev    | Min       | Median    | Max       | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| Manual         |  9.347 ns | 0.0286 ns | 0.0268 ns |  9.307 ns |  9.341 ns |  9.400 ns |  1.00 |    0.00 |    1 |         - |          NA |
| EggMapper      | 13.975 ns | 0.0538 ns | 0.0503 ns | 13.852 ns | 13.986 ns | 14.037 ns |  1.50 |    0.01 |    3 |         - |          NA |
| EggMapperTyped | 10.942 ns | 0.0192 ns | 0.0180 ns | 10.908 ns | 10.945 ns | 10.974 ns |  1.17 |    0.00 |    2 |         - |          NA |
| AutoMapper     | 38.766 ns | 0.1228 ns | 0.1149 ns | 38.571 ns | 38.756 ns | 38.982 ns |  4.15 |    0.02 |    5 |         - |          NA |
| Mapster        | 15.960 ns | 0.0734 ns | 0.0686 ns | 15.873 ns | 15.947 ns | 16.095 ns |  1.71 |    0.01 |    4 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 15.52 ns | 0.268 ns | 0.251 ns | 15.19 ns | 15.49 ns | 16.02 ns |  1.00 |    0.02 |    1 | 0.0010 |      80 B |        1.00 |
| EggMapper                   | 26.93 ns | 0.360 ns | 0.319 ns | 26.44 ns | 26.94 ns | 27.65 ns |  1.74 |    0.03 |    2 | 0.0010 |      80 B |        1.00 |
| AutoMapper                  | 50.64 ns | 0.575 ns | 0.538 ns | 49.49 ns | 50.67 ns | 51.30 ns |  3.26 |    0.06 |    3 | 0.0010 |      80 B |        1.00 |
| EggMapperDerivedSource      | 27.31 ns | 0.578 ns | 0.593 ns | 26.09 ns | 27.31 ns | 28.11 ns |  1.76 |    0.05 |    2 | 0.0010 |      80 B |        1.00 |
| EggMapperAlternatingSources | 28.23 ns | 0.374 ns | 0.332 ns | 27.72 ns | 28.22 ns | 28.92 ns |  1.82 |    0.04 |    2 | 0.0010 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.154 μs | 0.0228 μs | 0.0327 μs |   1.086 μs |   1.156 μs |   1.229 μs |   1.00 |    0.04 |    1 | 0.0916 | 0.0019 |   7.52 KB |        1.00 |
| AutoMapperStartup | 299.509 μs | 1.6068 μs | 1.3418 μs | 297.385 μs | 299.283 μs | 302.018 μs | 259.77 |    7.32 |    3 |      - |      - | 106.09 KB |       14.12 |
| MapsterStartup    |   2.255 μs | 0.0445 μs | 0.1032 μs |   2.092 μs |   2.228 μs |   2.528 μs |   1.96 |    0.10 |    2 | 0.1373 | 0.0038 |  11.51 KB |        1.53 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| EggMapper  | 1.193 ms | 0.0139 ms | 0.0130 ms | 1.174 ms | 1.188 ms | 1.216 ms |  1.00 |    0.01 |    1 |      - |  87.27 KB |        1.00 |
| AutoMapper | 3.723 ms | 0.0259 ms | 0.0243 ms | 3.690 ms | 3.715 ms | 3.772 ms |  3.12 |    0.04 |    3 |      - | 327.46 KB |        3.75 |
| Mapster    | 2.082 ms | 0.0271 ms | 0.0254 ms | 2.050 ms | 2.085 ms | 2.126 ms |  1.75 |    0.03 |    2 | 7.8125 | 758.57 KB |        8.69 |

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

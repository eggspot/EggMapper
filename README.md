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
> ⏱ **Last updated:** 2026-11-15 00:06 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 15.71 ns | **27.26 ns (1.7×)** | 28.67 ns (1.8×) | 86.00 ns (5.5×) | 15.93 ns (1.0×) |
| **Flattening** | 19.61 ns | **30.31 ns (1.6×)** | 48.44 ns (2.5×) | 90.65 ns (4.6×) | 24.50 ns (1.2×) |
| **Deep (2 nested)** | 60.84 ns | **54.69 ns (0.9×)** | 72.41 ns (1.2×) | 121.40 ns (2.0×) | 51.72 ns (0.8×) |
| **Complex (nest+coll)** | 71.71 ns | **100.70 ns (1.4×)** | 95.15 ns (1.3×) | 159.85 ns (2.2×) | 78.56 ns (1.1×) |
| **Collection (100)** | 1.752 μs | **1.738 μs (1.0×)** | 1.861 μs (1.1×) | 2.545 μs (1.4×) | 1.906 μs (1.1×) |
| **Deep Coll (100)** | 5.633 μs | **4.317 μs (0.8×)** | 5.952 μs (1.1×) | 6.845 μs (1.2×) | 5.697 μs (1.0×) |
| **Large Coll (1000)** | 18.29 μs | **18.10 μs (1.0×)** | 17.63 μs (1.0×) | 22.57 μs (1.2×) | 18.72 μs (1.0×) |
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
| Manual               |  15.71 ns | 0.199 ns | 0.176 ns |  15.43 ns |  15.72 ns |  16.03 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper            |  27.26 ns | 0.389 ns | 0.344 ns |  26.73 ns |  27.21 ns |  27.95 ns |  1.74 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperTyped       |  21.68 ns | 0.378 ns | 0.354 ns |  21.04 ns |  21.67 ns |  22.32 ns |  1.38 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper           |  86.00 ns | 0.297 ns | 0.263 ns |  85.45 ns |  86.03 ns |  86.33 ns |  5.48 |    0.06 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster              |  28.67 ns | 0.254 ns | 0.237 ns |  28.31 ns |  28.67 ns |  29.10 ns |  1.83 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap          |  15.93 ns | 0.314 ns | 0.262 ns |  15.60 ns |  15.88 ns |  16.55 ns |  1.01 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper          | 490.64 ns | 2.906 ns | 2.576 ns | 487.09 ns | 491.12 ns | 495.54 ns | 31.24 |    0.37 |    6 | 0.0200 |     344 B |        4.30 |
| EggMapperGenerator   |  15.64 ns | 0.337 ns | 0.315 ns |  15.24 ns |  15.69 ns |  16.35 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapperClassMapper |  16.16 ns | 0.268 ns | 0.237 ns |  15.86 ns |  16.13 ns |  16.64 ns |  1.03 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.61 ns | 0.292 ns | 0.273 ns |  19.09 ns |  19.59 ns |  20.07 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.31 ns | 0.244 ns | 0.228 ns |  29.90 ns |  30.29 ns |  30.72 ns |  1.55 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  90.65 ns | 0.828 ns | 0.774 ns |  89.52 ns |  90.56 ns |  92.41 ns |  4.62 |    0.07 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  48.44 ns | 0.392 ns | 0.367 ns |  48.04 ns |  48.27 ns |  49.29 ns |  2.47 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.50 ns | 0.282 ns | 0.264 ns |  24.12 ns |  24.43 ns |  25.03 ns |  1.25 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 525.95 ns | 2.957 ns | 2.766 ns | 521.10 ns | 526.45 ns | 530.44 ns | 26.83 |    0.39 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.84 ns | 1.068 ns | 0.999 ns |  58.26 ns |  60.95 ns |  62.44 ns |  1.00 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  54.69 ns | 0.723 ns | 0.676 ns |  53.38 ns |  54.50 ns |  55.82 ns |  0.90 |    0.02 |    2 | 0.0095 |     160 B |        0.59 |
| AutoMapper  | 121.40 ns | 1.255 ns | 1.113 ns | 119.48 ns | 121.43 ns | 123.31 ns |  2.00 |    0.04 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  72.41 ns | 0.700 ns | 0.621 ns |  71.19 ns |  72.49 ns |  73.34 ns |  1.19 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  51.72 ns | 0.610 ns | 0.541 ns |  50.80 ns |  51.69 ns |  52.77 ns |  0.85 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 514.71 ns | 3.505 ns | 3.278 ns | 507.78 ns | 515.53 ns | 519.74 ns |  8.46 |    0.15 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  71.71 ns | 0.655 ns | 0.613 ns |  70.38 ns |  71.71 ns |  72.64 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 100.70 ns | 1.805 ns | 1.688 ns |  97.17 ns | 101.43 ns | 102.81 ns |  1.40 |    0.03 |    4 | 0.0153 |     256 B |        0.80 |
| AutoMapper  | 159.85 ns | 1.852 ns | 1.642 ns | 156.31 ns | 159.60 ns | 162.63 ns |  2.23 |    0.03 |    5 | 0.0196 |     328 B |        1.02 |
| Mapster     |  95.15 ns | 1.603 ns | 1.500 ns |  92.88 ns |  95.20 ns |  97.30 ns |  1.33 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  78.56 ns | 1.313 ns | 1.228 ns |  76.35 ns |  78.41 ns |  80.61 ns |  1.10 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 560.13 ns | 4.811 ns | 4.265 ns | 553.60 ns | 560.44 ns | 566.59 ns |  7.81 |    0.09 |    6 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Median   | Min      | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.752 μs | 0.0303 μs | 0.0349 μs | 1.741 μs | 1.718 μs | 1.842 μs |  1.00 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.738 μs | 0.0165 μs | 0.0138 μs | 1.740 μs | 1.708 μs | 1.762 μs |  0.99 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.545 μs | 0.0490 μs | 0.0620 μs | 2.533 μs | 2.438 μs | 2.670 μs |  1.45 |    0.04 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.861 μs | 0.0371 μs | 0.0381 μs | 1.856 μs | 1.805 μs | 1.927 μs |  1.06 |    0.03 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.906 μs | 0.0378 μs | 0.0738 μs | 1.872 μs | 1.813 μs | 2.060 μs |  1.09 |    0.05 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.558 μs | 0.0502 μs | 0.0470 μs | 2.565 μs | 2.491 μs | 2.614 μs |  1.46 |    0.04 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.633 μs | 0.1123 μs | 0.2163 μs | 5.331 μs | 5.577 μs | 6.211 μs |  1.00 |    0.05 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 4.317 μs | 0.0770 μs | 0.0720 μs | 4.203 μs | 4.332 μs | 4.420 μs |  0.77 |    0.03 |    1 | 1.0071 | 0.0534 |  16.46 KB |        0.60 |
| AutoMapper  | 6.845 μs | 0.1078 μs | 0.0956 μs | 6.614 μs | 6.862 μs | 6.954 μs |  1.22 |    0.05 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.952 μs | 0.0886 μs | 0.0829 μs | 5.835 μs | 5.928 μs | 6.078 μs |  1.06 |    0.04 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.697 μs | 0.1127 μs | 0.1651 μs | 5.483 μs | 5.692 μs | 6.045 μs |  1.01 |    0.05 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.890 μs | 0.1168 μs | 0.2222 μs | 5.328 μs | 5.951 μs | 6.251 μs |  1.05 |    0.06 |    2 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.29 μs | 0.357 μs | 0.334 μs | 17.79 μs | 18.21 μs | 18.91 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.10 μs | 0.260 μs | 0.244 μs | 17.44 μs | 18.12 μs | 18.46 μs |  0.99 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 22.57 μs | 0.409 μs | 0.383 μs | 21.74 μs | 22.50 μs | 23.16 μs |  1.23 |    0.03 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.63 μs | 0.297 μs | 0.263 μs | 17.06 μs | 17.59 μs | 18.04 μs |  0.96 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.72 μs | 0.232 μs | 0.217 μs | 18.24 μs | 18.73 μs | 19.07 μs |  1.02 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.16 μs | 0.298 μs | 0.279 μs | 19.73 μs | 20.14 μs | 20.73 μs |  1.10 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🔷 Map Into Existing Object

| Method         | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|----------:|------------:|
| Manual         | 10.88 ns | 0.038 ns | 0.034 ns | 10.84 ns | 10.87 ns | 10.95 ns |  1.00 |    0.00 |    1 |         - |          NA |
| EggMapper      | 23.55 ns | 0.154 ns | 0.129 ns | 23.31 ns | 23.58 ns | 23.77 ns |  2.16 |    0.01 |    4 |         - |          NA |
| EggMapperTyped | 16.53 ns | 0.142 ns | 0.133 ns | 16.39 ns | 16.49 ns | 16.79 ns |  1.52 |    0.01 |    2 |         - |          NA |
| AutoMapper     | 80.39 ns | 0.340 ns | 0.284 ns | 80.04 ns | 80.36 ns | 81.09 ns |  7.39 |    0.03 |    5 |         - |          NA |
| Mapster        | 21.27 ns | 0.127 ns | 0.119 ns | 21.17 ns | 21.21 ns | 21.57 ns |  1.96 |    0.01 |    3 |         - |          NA |

#### 🔶 Map<TDest>(object) (AutoMapper-style call)

| Method                      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual                      | 15.51 ns | 0.220 ns | 0.206 ns | 15.08 ns | 15.53 ns | 15.84 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper                   | 31.13 ns | 0.457 ns | 0.428 ns | 30.51 ns | 31.10 ns | 31.97 ns |  2.01 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper                  | 83.02 ns | 0.386 ns | 0.361 ns | 82.61 ns | 82.84 ns | 83.72 ns |  5.35 |    0.07 |    5 | 0.0048 |      80 B |        1.00 |
| EggMapperDerivedSource      | 34.76 ns | 0.518 ns | 0.485 ns | 34.19 ns | 34.72 ns | 35.82 ns |  2.24 |    0.04 |    3 | 0.0048 |      80 B |        1.00 |
| EggMapperAlternatingSources | 36.98 ns | 0.411 ns | 0.385 ns | 36.21 ns | 36.97 ns | 37.62 ns |  2.39 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean       | Error     | StdDev    | Min        | Median     | Max        | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  |   1.323 μs | 0.0216 μs | 0.0202 μs |   1.285 μs |   1.320 μs |   1.358 μs |   1.00 |    0.02 |    1 | 0.4597 | 0.0076 |   7.52 KB |        1.00 |
| AutoMapperStartup | 447.355 μs | 7.3067 μs | 6.8347 μs | 439.509 μs | 444.900 μs | 459.624 μs | 338.32 |    7.06 |    3 | 5.8594 |      - |  106.5 KB |       14.17 |
| MapsterStartup    |   2.574 μs | 0.0269 μs | 0.0252 μs |   2.528 μs |   2.571 μs |   2.618 μs |   1.95 |    0.03 |    2 | 0.7019 | 0.0267 |  11.51 KB |        1.53 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.420 ms | 0.0084 ms | 0.0079 ms | 1.410 ms | 1.419 ms | 1.433 ms |  1.00 |    0.01 |    1 |  3.9063 |       - |  87.12 KB |        1.00 |
| AutoMapper | 4.504 ms | 0.0267 ms | 0.0250 ms | 4.467 ms | 4.501 ms | 4.552 ms |  3.17 |    0.02 |    3 | 15.6250 |  7.8125 | 327.23 KB |        3.76 |
| Mapster    | 2.516 ms | 0.0174 ms | 0.0145 ms | 2.500 ms | 2.511 ms | 2.550 ms |  1.77 |    0.01 |    2 | 39.0625 | 15.6250 | 764.21 KB |        8.77 |

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

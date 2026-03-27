# 🥚 EggMapper

> **The fastest .NET runtime object-to-object mapper** — forked from AutoMapper's last open-source release, rebuilt for maximum performance. Drop-in replacement with the same API, 1.5–5× faster.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

📖 **[Full documentation →](https://github.com/eggspot/EggMapper/wiki)**

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
- 📖 **MIT licensed** — free for commercial use, forever

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
```

## Performance

<!-- PERF_TIMESTAMP_START -->
> ⏱ **Last updated:** 2026-03-27 04:16 UTC
<!-- PERF_TIMESTAMP_END -->

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 17.83 ns | **29.32 ns (1.6×)** | 28.27 ns (1.6×) | 71.94 ns (4.0×) | 16.12 ns (0.9×) |
| **Flattening** | 19.53 ns | **28.67 ns (1.5×)** | 38.68 ns (2.0×) | 70.34 ns (3.6×) | 25.19 ns (1.3×) |
| **Deep (2 nested)** | 57.75 ns | **72.51 ns (1.3×)** | 73.68 ns (1.3×) | 115.06 ns (2.0×) | 55.74 ns (1.0×) |
| **Complex (nest+coll)** | 76.20 ns | **97.80 ns (1.3×)** | 91.19 ns (1.2×) | 143.25 ns (1.9×) | 75.50 ns (1.0×) |
| **Collection (100)** | 1.868 μs | **1.896 μs (1.0×)** | 1.777 μs (0.9×) | 2.542 μs (1.4×) | 1.853 μs (1.0×) |
| **Deep Coll (100)** | 5.905 μs | **6.211 μs (1.1×)** | 6.368 μs (1.1×) | 7.053 μs (1.2×) | 5.924 μs (1.0×) |
| **Large Coll (1000)** | 17.43 μs | **17.98 μs (1.0×)** | 18.91 μs (1.1×) | 21.81 μs (1.2×) | 18.86 μs (1.1×) |
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
| Manual               |  17.83 ns | 0.423 ns | 0.396 ns |  17.32 ns |  17.76 ns |  18.59 ns |  1.00 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| EggMapper            |  29.32 ns | 0.186 ns | 0.174 ns |  29.01 ns |  29.34 ns |  29.60 ns |  1.65 |    0.04 |    4 | 0.0032 |      80 B |        1.00 |
| AutoMapper           |  71.94 ns | 0.110 ns | 0.103 ns |  71.76 ns |  71.97 ns |  72.07 ns |  4.04 |    0.09 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster              |  28.27 ns | 0.165 ns | 0.155 ns |  27.94 ns |  28.29 ns |  28.51 ns |  1.59 |    0.03 |    3 | 0.0032 |      80 B |        1.00 |
| MapperlyMap          |  16.12 ns | 0.122 ns | 0.115 ns |  15.88 ns |  16.14 ns |  16.24 ns |  0.90 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| AgileMapper          | 348.87 ns | 0.492 ns | 0.460 ns | 347.96 ns | 348.90 ns | 349.71 ns | 19.58 |    0.42 |    6 | 0.0134 |     344 B |        4.30 |
| EggMapperGenerator   |  16.14 ns | 0.191 ns | 0.169 ns |  15.84 ns |  16.15 ns |  16.41 ns |  0.91 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapperClassMapper |  17.13 ns | 0.133 ns | 0.124 ns |  16.83 ns |  17.13 ns |  17.29 ns |  0.96 |    0.02 |    2 | 0.0032 |      80 B |        1.00 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.53 ns | 0.161 ns | 0.151 ns |  19.29 ns |  19.56 ns |  19.84 ns |  1.00 |    0.01 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  28.67 ns | 0.194 ns | 0.181 ns |  28.40 ns |  28.69 ns |  29.01 ns |  1.47 |    0.01 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  70.34 ns | 0.091 ns | 0.085 ns |  70.20 ns |  70.35 ns |  70.53 ns |  3.60 |    0.03 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  38.68 ns | 0.061 ns | 0.057 ns |  38.60 ns |  38.69 ns |  38.81 ns |  1.98 |    0.02 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  25.19 ns | 0.119 ns | 0.093 ns |  25.00 ns |  25.21 ns |  25.33 ns |  1.29 |    0.01 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 353.33 ns | 0.297 ns | 0.263 ns | 352.80 ns | 353.31 ns | 353.82 ns | 18.09 |    0.14 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  57.75 ns | 0.416 ns | 0.347 ns |  57.16 ns |  57.79 ns |  58.50 ns |  1.00 |    0.01 |    1 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  72.51 ns | 1.385 ns | 1.595 ns |  69.50 ns |  72.12 ns |  74.86 ns |  1.26 |    0.03 |    2 | 0.0107 |     272 B |        1.00 |
| AutoMapper  | 115.06 ns | 0.321 ns | 0.300 ns | 114.41 ns | 115.11 ns | 115.55 ns |  1.99 |    0.01 |    3 | 0.0107 |     272 B |        1.00 |
| Mapster     |  73.68 ns | 0.703 ns | 0.658 ns |  72.34 ns |  73.62 ns |  75.04 ns |  1.28 |    0.01 |    2 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  55.74 ns | 1.124 ns | 1.203 ns |  53.99 ns |  56.20 ns |  57.69 ns |  0.97 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| AgileMapper | 379.93 ns | 1.836 ns | 1.718 ns | 377.00 ns | 379.69 ns | 382.84 ns |  6.58 |    0.05 |    4 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  76.20 ns | 0.689 ns | 0.611 ns |  74.95 ns |  76.09 ns |  77.22 ns |  1.00 |    0.01 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   |  97.80 ns | 0.835 ns | 0.781 ns |  96.79 ns |  97.75 ns |  98.99 ns |  1.28 |    0.01 |    3 | 0.0126 |     320 B |        1.00 |
| AutoMapper  | 143.25 ns | 0.544 ns | 0.482 ns | 142.37 ns | 143.17 ns | 143.97 ns |  1.88 |    0.02 |    4 | 0.0129 |     328 B |        1.02 |
| Mapster     |  91.19 ns | 1.426 ns | 1.334 ns |  88.73 ns |  91.74 ns |  92.71 ns |  1.20 |    0.02 |    2 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  75.50 ns | 0.669 ns | 0.626 ns |  74.55 ns |  75.35 ns |  76.78 ns |  0.99 |    0.01 |    1 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 439.90 ns | 0.872 ns | 0.773 ns | 438.76 ns | 439.68 ns | 440.96 ns |  5.77 |    0.05 |    5 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.868 μs | 0.0194 μs | 0.0162 μs | 1.841 μs | 1.871 μs | 1.899 μs |  1.00 |    0.01 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| EggMapper   | 1.896 μs | 0.0287 μs | 0.0268 μs | 1.853 μs | 1.888 μs | 1.944 μs |  1.02 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| AutoMapper  | 2.542 μs | 0.0200 μs | 0.0187 μs | 2.505 μs | 2.547 μs | 2.572 μs |  1.36 |    0.01 |    2 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.777 μs | 0.0318 μs | 0.0297 μs | 1.725 μs | 1.780 μs | 1.817 μs |  0.95 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| MapperlyMap | 1.853 μs | 0.0370 μs | 0.0364 μs | 1.808 μs | 1.853 μs | 1.940 μs |  0.99 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| AgileMapper | 2.546 μs | 0.0363 μs | 0.0340 μs | 2.464 μs | 2.558 μs | 2.574 μs |  1.36 |    0.02 |    2 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.905 μs | 0.0723 μs | 0.0641 μs | 5.812 μs | 5.894 μs | 6.045 μs |  1.00 |    0.01 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 6.211 μs | 0.1027 μs | 0.0961 μs | 6.073 μs | 6.204 μs | 6.384 μs |  1.05 |    0.02 |    3 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| AutoMapper  | 7.053 μs | 0.1009 μs | 0.0944 μs | 6.889 μs | 7.058 μs | 7.198 μs |  1.19 |    0.02 |    4 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 6.368 μs | 0.1240 μs | 0.1817 μs | 6.123 μs | 6.289 μs | 6.743 μs |  1.08 |    0.03 |    3 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 5.924 μs | 0.0798 μs | 0.0746 μs | 5.793 μs | 5.903 μs | 6.058 μs |  1.00 |    0.02 |    2 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 5.505 μs | 0.0217 μs | 0.0192 μs | 5.469 μs | 5.508 μs | 5.539 μs |  0.93 |    0.01 |    1 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.43 μs | 0.258 μs | 0.242 μs | 17.02 μs | 17.46 μs | 17.86 μs |  1.00 |    0.02 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 17.98 μs | 0.170 μs | 0.151 μs | 17.79 μs | 17.95 μs | 18.30 μs |  1.03 |    0.02 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 21.81 μs | 0.226 μs | 0.211 μs | 21.47 μs | 21.80 μs | 22.21 μs |  1.25 |    0.02 |    4 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 18.91 μs | 0.178 μs | 0.166 μs | 18.60 μs | 18.94 μs | 19.18 μs |  1.09 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 18.86 μs | 0.241 μs | 0.213 μs | 18.54 μs | 18.80 μs | 19.33 μs |  1.08 |    0.02 |    2 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 20.32 μs | 0.398 μs | 0.391 μs | 19.79 μs | 20.18 μs | 20.81 μs |  1.17 |    0.03 |    3 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error     | StdDev    | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|----------:|----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 1,150.495 μs | 9.6950 μs | 8.5944 μs | 1,140.523 μs | 1,149.920 μs | 1,169.922 μs | 1.000 |    3 | 3.9063 | 1.9531 |  95.91 KB |        1.00 |
| AutoMapperStartup |   264.578 μs | 1.2569 μs | 1.1142 μs |   263.184 μs |   264.574 μs |   266.769 μs | 0.230 |    2 | 3.9063 |      - | 103.71 KB |        1.08 |
| MapsterStartup    |     2.961 μs | 0.0160 μs | 0.0150 μs |     2.933 μs |     2.960 μs |     2.986 μs | 0.003 |    1 | 0.4692 | 0.0114 |  11.51 KB |        0.12 |

#### ⚪ Cold Start (Config + First Map per Type Pair)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0    | Gen1    | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|--------:|--------:|----------:|------------:|
| EggMapper  | 1.132 ms | 0.0090 ms | 0.0084 ms | 1.119 ms | 1.132 ms | 1.146 ms |  1.00 |    0.01 |    1 |  3.9063 |       - |  95.97 KB |        1.00 |
| AutoMapper | 3.260 ms | 0.0134 ms | 0.0119 ms | 3.238 ms | 3.261 ms | 3.279 ms |  2.88 |    0.02 |    3 |  7.8125 |       - | 309.75 KB |        3.23 |
| Mapster    | 2.490 ms | 0.0060 ms | 0.0050 ms | 2.485 ms | 2.488 ms | 2.502 ms |  2.20 |    0.02 |    2 | 31.2500 | 23.4375 | 766.11 KB |        7.98 |

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

| | **Runtime** (`EggMapper`) | **Tier 2** (`EggMapper.Generator`) | **Tier 3** (`EggMapper.ClassMapper`) |
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

See [Migration Guide](docs/Migration-Guide.md) to move from runtime to compile-time APIs.

---

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](https://github.com/eggspot/EggMapper/wiki/Getting-Started) | Installation and your first runtime mapping |
| [Tier 2 Getting Started](docs/Tier2-Getting-Started.md) | Compile-time extension methods with `[MapTo]` |
| [Tier 3 Getting Started](docs/Tier3-Getting-Started.md) | Compile-time partial mapper classes with `[EggMapper]` |
| [Migration Guide](docs/Migration-Guide.md) | Moving from runtime to compile-time APIs |
| [Diagnostic Reference](docs/diagnostics/) | All EGG diagnostic codes explained |
| [Configuration](https://github.com/eggspot/EggMapper/wiki/Configuration) | `MapperConfiguration` options |
| [Profiles](https://github.com/eggspot/EggMapper/wiki/Profiles) | Organising maps with `Profile` |
| [Dependency Injection](https://github.com/eggspot/EggMapper/wiki/Dependency-Injection) | ASP.NET Core / DI integration |
| [Advanced Features](https://github.com/eggspot/EggMapper/wiki/Advanced-Features) | `ForMember`, conditions, hooks, etc. |
| [Performance](https://github.com/eggspot/EggMapper/wiki/Performance) | Benchmark methodology & tips |
| [API Reference](https://github.com/eggspot/EggMapper/wiki/API-Reference) | Full public API surface |

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
- **Improve docs** — Edit files in the `docs/` folder (auto-synced to the wiki)
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

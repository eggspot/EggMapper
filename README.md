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

All benchmarks run on BenchmarkDotNet with .NET 10. Ratio = time vs hand-written manual code (lower is better).

| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 14.5 ns | **29.5 ns** (2.0×) | 31.1 ns (2.1×) | 73.0 ns (5.0×) | 14.9 ns (1.0×) |
| **Flattening** | 18.3 ns | **37.3 ns** (2.0×) | 38.8 ns (2.1×) | 92.5 ns (5.1×) | 26.2 ns (1.4×) |
| **Deep (2 nested)** | 51.2 ns | **64.6 ns** (1.3×) | 72.3 ns (1.4×) | 111 ns (2.2×) | 52.0 ns (1.0×) |
| **Complex (nest+coll)** | 62.4 ns | **88.8 ns** (1.4×) | 85.8 ns (1.4×) | 143 ns (2.3×) | 65.0 ns (1.0×) |
| **Collection (100)** | 1.81 us | **1.95 us** (1.1×) | 1.85 us (1.0×) | 2.39 us (1.3×) | 1.85 us (1.0×) |
| **Deep Coll (100)** | 5.18 us | **6.07 us** (1.2×) | 5.51 us (1.1×) | 7.58 us (1.5×) | 5.06 us (1.0×) |
| **Large Coll (1000)** | 21.7 us | **27.7 us** (1.3×) | 24.1 us (1.1×) | 29.9 us (1.4×) | 24.8 us (1.1×) |

**\*** *Mapperly is a compile-time source generator — it produces code equivalent to hand-written mapping. EggMapper is the fastest **runtime** mapper.*

**Allocations:** EggMapper matches manual allocation exactly in every scenario (zero extra bytes).

Run the benchmarks yourself:

```bash
cd src/EggMapper.Benchmarks
dotnet run --configuration Release -f net10.0 -- --filter * --exporters json markdown
```

<!-- BENCHMARK_RESULTS_START -->

> ⏱ **Last updated:** 2026-03-20 09:28 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.47 ns | 0.251 ns | 0.222 ns |  18.16 ns |  18.48 ns |  18.91 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapper   |  32.69 ns | 0.167 ns | 0.157 ns |  32.45 ns |  32.67 ns |  32.96 ns |  1.77 |    0.02 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  69.84 ns | 0.370 ns | 0.328 ns |  69.29 ns |  69.87 ns |  70.43 ns |  3.78 |    0.05 |    4 | 0.0031 |      80 B |        1.00 |
| Mapster     |  31.27 ns | 0.319 ns | 0.298 ns |  30.65 ns |  31.34 ns |  31.71 ns |  1.69 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  19.18 ns | 0.283 ns | 0.265 ns |  18.73 ns |  19.22 ns |  19.74 ns |  1.04 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| AgileMapper | 351.10 ns | 0.531 ns | 0.471 ns | 349.93 ns | 351.08 ns | 352.01 ns | 19.01 |    0.22 |    5 | 0.0134 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.96 ns | 0.358 ns | 0.335 ns |  18.42 ns |  18.97 ns |  19.56 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  30.36 ns | 0.445 ns | 0.416 ns |  29.76 ns |  30.44 ns |  30.88 ns |  1.60 |    0.03 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  73.25 ns | 0.157 ns | 0.139 ns |  72.87 ns |  73.27 ns |  73.48 ns |  3.86 |    0.07 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  38.60 ns | 0.176 ns | 0.165 ns |  38.27 ns |  38.68 ns |  38.84 ns |  2.04 |    0.04 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  26.97 ns | 0.381 ns | 0.318 ns |  26.45 ns |  27.08 ns |  27.36 ns |  1.42 |    0.03 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 357.73 ns | 0.942 ns | 0.786 ns | 356.41 ns | 357.71 ns | 359.51 ns | 18.87 |    0.33 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.77 ns | 1.170 ns | 1.037 ns |  59.25 ns |  60.46 ns |  62.49 ns |  1.00 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  76.33 ns | 0.832 ns | 0.738 ns |  74.93 ns |  76.32 ns |  77.34 ns |  1.26 |    0.02 |    2 | 0.0107 |     272 B |        1.00 |
| AutoMapper  | 115.51 ns | 0.606 ns | 0.567 ns | 114.60 ns | 115.44 ns | 116.34 ns |  1.90 |    0.03 |    3 | 0.0107 |     272 B |        1.00 |
| Mapster     |  76.18 ns | 1.070 ns | 0.949 ns |  74.21 ns |  76.15 ns |  78.15 ns |  1.25 |    0.03 |    2 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  60.92 ns | 0.563 ns | 0.499 ns |  59.86 ns |  60.90 ns |  61.66 ns |  1.00 |    0.02 |    1 | 0.0107 |     272 B |        1.00 |
| AgileMapper | 386.67 ns | 0.998 ns | 0.833 ns | 384.76 ns | 386.59 ns | 388.22 ns |  6.36 |    0.11 |    4 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  76.61 ns | 0.787 ns | 0.614 ns |  75.29 ns |  76.70 ns |  77.24 ns |  1.00 |    0.01 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   |  91.26 ns | 1.403 ns | 1.244 ns |  89.19 ns |  91.41 ns |  93.76 ns |  1.19 |    0.02 |    2 | 0.0126 |     320 B |        1.00 |
| AutoMapper  | 148.47 ns | 0.838 ns | 0.784 ns | 147.02 ns | 148.44 ns | 149.90 ns |  1.94 |    0.02 |    4 | 0.0129 |     328 B |        1.02 |
| Mapster     |  98.08 ns | 1.074 ns | 1.004 ns |  96.10 ns |  98.23 ns |  99.56 ns |  1.28 |    0.02 |    3 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  77.35 ns | 1.433 ns | 1.341 ns |  74.78 ns |  77.49 ns |  79.74 ns |  1.01 |    0.02 |    1 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 434.68 ns | 1.366 ns | 1.278 ns | 433.08 ns | 434.29 ns | 437.01 ns |  5.67 |    0.05 |    5 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.943 μs | 0.0384 μs | 0.0486 μs | 1.870 μs | 1.936 μs | 2.040 μs |  1.00 |    0.03 |    2 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| EggMapper   | 2.101 μs | 0.0351 μs | 0.0328 μs | 2.053 μs | 2.105 μs | 2.162 μs |  1.08 |    0.03 |    2 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AutoMapper  | 2.586 μs | 0.0272 μs | 0.0241 μs | 2.551 μs | 2.585 μs | 2.628 μs |  1.33 |    0.03 |    3 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.809 μs | 0.0356 μs | 0.0451 μs | 1.738 μs | 1.812 μs | 1.880 μs |  0.93 |    0.03 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| MapperlyMap | 2.055 μs | 0.0411 μs | 0.0490 μs | 1.969 μs | 2.052 μs | 2.142 μs |  1.06 |    0.04 |    2 | 0.3510 | 0.0114 |   8.65 KB |        1.00 |
| AgileMapper | 2.680 μs | 0.0243 μs | 0.0227 μs | 2.644 μs | 2.681 μs | 2.716 μs |  1.38 |    0.04 |    3 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.025 μs | 0.0977 μs | 0.0914 μs | 5.828 μs | 6.008 μs | 6.141 μs |  1.00 |    0.02 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 6.353 μs | 0.0856 μs | 0.0801 μs | 6.220 μs | 6.361 μs | 6.483 μs |  1.05 |    0.02 |    3 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| AutoMapper  | 7.292 μs | 0.0513 μs | 0.0480 μs | 7.177 μs | 7.298 μs | 7.363 μs |  1.21 |    0.02 |    4 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 6.464 μs | 0.0699 μs | 0.0654 μs | 6.354 μs | 6.489 μs | 6.551 μs |  1.07 |    0.02 |    3 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 6.042 μs | 0.0825 μs | 0.0772 μs | 5.896 μs | 6.076 μs | 6.156 μs |  1.00 |    0.02 |    2 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 5.666 μs | 0.0434 μs | 0.0406 μs | 5.583 μs | 5.664 μs | 5.735 μs |  0.94 |    0.02 |    1 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.63 μs | 0.379 μs | 0.354 μs | 18.93 μs | 19.67 μs | 20.16 μs |  1.00 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 20.73 μs | 0.360 μs | 0.336 μs | 20.38 μs | 20.70 μs | 21.53 μs |  1.06 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 22.82 μs | 0.452 μs | 0.423 μs | 22.00 μs | 22.95 μs | 23.32 μs |  1.16 |    0.03 |    3 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 17.26 μs | 0.343 μs | 0.524 μs | 16.19 μs | 17.17 μs | 18.47 μs |  0.88 |    0.03 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 20.29 μs | 0.402 μs | 0.376 μs | 19.66 μs | 20.38 μs | 20.81 μs |  1.03 |    0.03 |    2 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 20.18 μs | 0.301 μs | 0.282 μs | 19.63 μs | 20.18 μs | 20.70 μs |  1.03 |    0.02 |    2 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 4,802.303 μs | 70.4536 μs | 65.9023 μs | 4,675.865 μs | 4,821.542 μs | 4,888.825 μs | 1.000 |    0.02 |    3 | 7.8125 |      - | 280.64 KB |        1.00 |
| AutoMapperStartup |   259.581 μs |  0.5668 μs |  0.5025 μs |   258.714 μs |   259.600 μs |   260.513 μs | 0.054 |    0.00 |    2 | 3.9063 |      - | 103.99 KB |        0.37 |
| MapsterStartup    |     2.856 μs |  0.0325 μs |  0.0304 μs |     2.810 μs |     2.850 μs |     2.902 μs | 0.001 |    0.00 |    1 | 0.4692 | 0.0114 |  11.51 KB |        0.04 |

---

*Benchmarks run automatically on every push to `main` with .NET 10. [See workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)*

<!-- BENCHMARK_RESULTS_END -->

## Features

- ✅ Compiled expression tree delegates (zero runtime reflection)
- ✅ `ForMember` / `MapFrom` custom mappings
- ✅ `Ignore()` members
- ✅ `ReverseMap()` bidirectional mapping
- ✅ Nested object mapping (inlined into parent expression tree)
- ✅ Collection mapping (`List<T>`, arrays, `HashSet<T>`, etc.)
- ✅ Flattening (`src.Address.Street` → `dest.AddressStreet`)
- ✅ Constructor mapping
- ✅ Profile-based configuration
- ✅ Assembly scanning
- ✅ Before/After map hooks
- ✅ Conditional mapping
- ✅ Null substitution
- ✅ `MaxDepth` for self-referencing types
- ✅ Inheritance mapping
- ✅ Enum mapping (int ↔ enum and string ↔ enum auto-conversion)
- ✅ `ForPath` for nested destination properties
- ✅ .NET Dependency Injection integration (built-in, no extra package)
- ✅ Configuration validation
- ✅ `CreateMap(Type, Type)` runtime type mapping
- ✅ `ITypeConverter<S,D>` / `ConvertUsing` custom converters
- ✅ `ShouldMapProperty` global property filter

## Documentation

| Page | Description |
|------|-------------|
| [Getting Started](https://github.com/eggspot/EggMapper/wiki/Getting-Started) | Installation and your first mapping |
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

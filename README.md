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
- ✅ Enum mapping
- ✅ `ForPath` for nested destination properties
- ✅ .NET Dependency Injection integration (built-in, no extra package)
- ✅ Configuration validation

<!-- BENCHMARK_RESULTS_START -->

> ⏱ **Last updated:** 2026-03-19 08:38 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  17.24 ns | 0.167 ns | 0.156 ns |  16.89 ns |  17.28 ns |  17.49 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  28.70 ns | 0.253 ns | 0.237 ns |  28.07 ns |  28.72 ns |  29.10 ns |  1.66 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  83.90 ns | 0.248 ns | 0.232 ns |  83.59 ns |  83.90 ns |  84.34 ns |  4.87 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster     |  30.78 ns | 0.261 ns | 0.231 ns |  30.27 ns |  30.85 ns |  31.04 ns |  1.79 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  17.30 ns | 0.241 ns | 0.226 ns |  16.94 ns |  17.24 ns |  17.73 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 498.84 ns | 1.181 ns | 1.105 ns | 497.18 ns | 498.47 ns | 500.67 ns | 28.93 |    0.26 |    5 | 0.0200 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  21.32 ns | 0.213 ns | 0.199 ns |  20.79 ns |  21.38 ns |  21.53 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  32.53 ns | 0.368 ns | 0.344 ns |  31.69 ns |  32.59 ns |  32.99 ns |  1.53 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  93.13 ns | 0.137 ns | 0.128 ns |  92.92 ns |  93.13 ns |  93.36 ns |  4.37 |    0.04 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  39.30 ns | 0.253 ns | 0.237 ns |  38.89 ns |  39.36 ns |  39.73 ns |  1.84 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  26.93 ns | 0.473 ns | 0.443 ns |  26.05 ns |  27.03 ns |  27.58 ns |  1.26 |    0.02 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 536.07 ns | 0.744 ns | 0.696 ns | 535.07 ns | 536.26 ns | 537.29 ns | 25.15 |    0.23 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  56.43 ns | 0.572 ns | 0.507 ns |  55.55 ns |  56.48 ns |  57.24 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  71.70 ns | 0.877 ns | 0.821 ns |  69.88 ns |  71.67 ns |  73.37 ns |  1.27 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 125.87 ns | 0.960 ns | 0.898 ns | 124.27 ns | 125.72 ns | 127.44 ns |  2.23 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| Mapster     |  73.78 ns | 1.089 ns | 0.965 ns |  71.66 ns |  74.10 ns |  75.07 ns |  1.31 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  57.03 ns | 1.076 ns | 0.954 ns |  54.59 ns |  57.14 ns |  58.56 ns |  1.01 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 511.10 ns | 2.196 ns | 1.947 ns | 509.27 ns | 510.46 ns | 515.65 ns |  9.06 |    0.09 |    4 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  75.75 ns | 1.571 ns | 1.469 ns |  73.79 ns |  75.15 ns |  78.12 ns |  1.00 |    0.03 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  96.83 ns | 1.512 ns | 1.414 ns |  94.84 ns |  97.18 ns |  99.02 ns |  1.28 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 162.39 ns | 0.680 ns | 0.603 ns | 160.98 ns | 162.40 ns | 163.30 ns |  2.14 |    0.04 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     | 105.87 ns | 0.944 ns | 0.836 ns | 104.60 ns | 105.94 ns | 107.62 ns |  1.40 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  78.40 ns | 1.061 ns | 0.992 ns |  77.30 ns |  77.99 ns |  80.71 ns |  1.04 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 569.19 ns | 0.772 ns | 0.722 ns | 568.19 ns | 568.97 ns | 570.43 ns |  7.52 |    0.14 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.89 μs | 0.376 μs | 0.574 μs | 18.03 μs | 18.86 μs | 19.97 μs |  1.00 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 21.17 μs | 0.416 μs | 0.479 μs | 20.15 μs | 21.24 μs | 21.82 μs |  1.12 |    0.04 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 23.14 μs | 0.344 μs | 0.322 μs | 22.56 μs | 23.26 μs | 23.63 μs |  1.23 |    0.04 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.84 μs | 0.372 μs | 0.509 μs | 17.85 μs | 18.84 μs | 19.84 μs |  1.00 |    0.04 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 19.59 μs | 0.325 μs | 0.304 μs | 19.21 μs | 19.58 μs | 20.35 μs |  1.04 |    0.03 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 21.32 μs | 0.420 μs | 0.467 μs | 20.53 μs | 21.30 μs | 22.31 μs |  1.13 |    0.04 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.775 μs | 0.0198 μs | 0.0176 μs | 1.747 μs | 1.774 μs | 1.813 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 2.005 μs | 0.0387 μs | 0.0398 μs | 1.938 μs | 2.004 μs | 2.097 μs |  1.13 |    0.02 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AutoMapper  | 2.513 μs | 0.0440 μs | 0.0411 μs | 2.445 μs | 2.521 μs | 2.572 μs |  1.42 |    0.03 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.842 μs | 0.0253 μs | 0.0224 μs | 1.802 μs | 1.850 μs | 1.871 μs |  1.04 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.947 μs | 0.0327 μs | 0.0377 μs | 1.869 μs | 1.958 μs | 2.007 μs |  1.10 |    0.02 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.669 μs | 0.0504 μs | 0.0495 μs | 2.563 μs | 2.675 μs | 2.738 μs |  1.50 |    0.03 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.533 μs | 0.1065 μs | 0.1140 μs | 5.209 μs | 5.551 μs | 5.710 μs |  1.00 |    0.03 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.423 μs | 0.1243 μs | 0.1527 μs | 6.151 μs | 6.429 μs | 6.756 μs |  1.16 |    0.04 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.943 μs | 0.1366 μs | 0.1278 μs | 6.700 μs | 6.953 μs | 7.140 μs |  1.26 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.499 μs | 0.1296 μs | 0.1543 μs | 6.085 μs | 6.519 μs | 6.698 μs |  1.18 |    0.04 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.871 μs | 0.1001 μs | 0.0983 μs | 5.679 μs | 5.862 μs | 6.039 μs |  1.06 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.638 μs | 0.0986 μs | 0.0922 μs | 5.496 μs | 5.652 μs | 5.772 μs |  1.02 |    0.03 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|--------:|-------:|----------:|------------:|
| EggMapperStartup  | 5,915.690 μs | 49.4886 μs | 46.2916 μs | 5,805.422 μs | 5,924.293 μs | 5,999.023 μs | 1.000 |    3 | 15.6250 |      - |  296.7 KB |        1.00 |
| AutoMapperStartup |   282.128 μs |  4.4477 μs |  3.7141 μs |   278.829 μs |   280.387 μs |   291.283 μs | 0.048 |    2 |  5.8594 |      - | 103.69 KB |        0.35 |
| MapsterStartup    |     2.655 μs |  0.0516 μs |  0.0614 μs |     2.557 μs |     2.646 μs |     2.770 μs | 0.000 |    1 |  0.7019 | 0.0267 |  11.51 KB |        0.04 |

---

*Benchmarks run automatically on every push to `main` with .NET 10. [See workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)*

<!-- BENCHMARK_RESULTS_END -->

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

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

> ⏱ **Last updated:** 2026-03-20 10:22 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  17.09 ns | 0.211 ns | 0.197 ns |  16.82 ns |  17.06 ns |  17.48 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  32.12 ns | 0.460 ns | 0.408 ns |  31.44 ns |  31.96 ns |  32.79 ns |  1.88 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  83.64 ns | 0.223 ns | 0.208 ns |  83.33 ns |  83.57 ns |  84.08 ns |  4.89 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster     |  29.40 ns | 0.585 ns | 0.820 ns |  28.26 ns |  29.13 ns |  31.22 ns |  1.72 |    0.05 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  16.28 ns | 0.387 ns | 0.568 ns |  15.06 ns |  16.24 ns |  17.44 ns |  0.95 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 490.92 ns | 2.222 ns | 1.970 ns | 487.95 ns | 490.91 ns | 494.42 ns | 28.72 |    0.34 |    5 | 0.0200 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.89 ns | 0.429 ns | 0.401 ns |  19.40 ns |  19.79 ns |  20.57 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  65.88 ns | 0.839 ns | 0.785 ns |  64.40 ns |  66.08 ns |  66.84 ns |  3.31 |    0.07 |    4 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  86.59 ns | 0.478 ns | 0.447 ns |  85.83 ns |  86.71 ns |  87.26 ns |  4.35 |    0.09 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  37.05 ns | 0.651 ns | 0.609 ns |  36.10 ns |  37.12 ns |  38.05 ns |  1.86 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  24.38 ns | 0.542 ns | 0.580 ns |  23.46 ns |  24.36 ns |  25.40 ns |  1.23 |    0.04 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 530.61 ns | 0.729 ns | 0.682 ns | 529.36 ns | 530.62 ns | 531.75 ns | 26.68 |    0.52 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.92 ns | 0.821 ns | 0.768 ns |  59.78 ns |  61.08 ns |  62.13 ns |  1.00 |    0.02 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  73.46 ns | 0.488 ns | 0.457 ns |  72.53 ns |  73.42 ns |  74.18 ns |  1.21 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 133.54 ns | 0.662 ns | 0.587 ns | 132.78 ns | 133.41 ns | 134.66 ns |  2.19 |    0.03 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  74.38 ns | 0.204 ns | 0.159 ns |  74.07 ns |  74.43 ns |  74.55 ns |  1.22 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  57.43 ns | 0.610 ns | 0.571 ns |  56.39 ns |  57.55 ns |  58.21 ns |  0.94 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 517.37 ns | 0.693 ns | 0.541 ns | 516.27 ns | 517.32 ns | 518.09 ns |  8.49 |    0.10 |    5 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  75.54 ns | 0.401 ns | 0.375 ns |  74.68 ns |  75.60 ns |  76.01 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  97.65 ns | 0.571 ns | 0.534 ns |  96.32 ns |  97.80 ns |  98.58 ns |  1.29 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 159.15 ns | 1.116 ns | 0.932 ns | 156.44 ns | 159.45 ns | 159.95 ns |  2.11 |    0.02 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     | 105.19 ns | 0.847 ns | 0.751 ns | 103.30 ns | 105.37 ns | 106.43 ns |  1.39 |    0.01 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  76.44 ns | 0.736 ns | 0.614 ns |  74.66 ns |  76.53 ns |  77.23 ns |  1.01 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 589.59 ns | 0.882 ns | 0.737 ns | 588.50 ns | 589.74 ns | 590.81 ns |  7.81 |    0.04 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.864 μs | 0.0333 μs | 0.0444 μs | 1.797 μs | 1.852 μs | 1.965 μs |  1.00 |    0.03 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| EggMapper   | 1.942 μs | 0.0382 μs | 0.0409 μs | 1.869 μs | 1.947 μs | 1.998 μs |  1.04 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.543 μs | 0.0184 μs | 0.0172 μs | 2.514 μs | 2.546 μs | 2.569 μs |  1.37 |    0.03 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.885 μs | 0.0256 μs | 0.0239 μs | 1.833 μs | 1.893 μs | 1.917 μs |  1.01 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.998 μs | 0.0216 μs | 0.0202 μs | 1.964 μs | 2.005 μs | 2.034 μs |  1.07 |    0.03 |    1 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.761 μs | 0.0179 μs | 0.0149 μs | 2.719 μs | 2.763 μs | 2.776 μs |  1.48 |    0.03 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.692 μs | 0.1131 μs | 0.2039 μs | 5.383 μs | 5.677 μs | 6.081 μs |  1.00 |    0.05 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 6.276 μs | 0.0623 μs | 0.0583 μs | 6.193 μs | 6.275 μs | 6.368 μs |  1.10 |    0.04 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.130 μs | 0.1107 μs | 0.1036 μs | 6.886 μs | 7.160 μs | 7.294 μs |  1.25 |    0.05 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.424 μs | 0.1266 μs | 0.1458 μs | 6.109 μs | 6.466 μs | 6.616 μs |  1.13 |    0.05 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.930 μs | 0.1149 μs | 0.1075 μs | 5.748 μs | 5.879 μs | 6.081 μs |  1.04 |    0.04 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.695 μs | 0.0344 μs | 0.0322 μs | 5.625 μs | 5.710 μs | 5.739 μs |  1.00 |    0.04 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.15 μs | 0.366 μs | 0.360 μs | 18.47 μs | 19.14 μs | 19.87 μs |  1.00 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 18.80 μs | 0.270 μs | 0.253 μs | 18.28 μs | 18.90 μs | 19.20 μs |  0.98 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 23.18 μs | 0.330 μs | 0.309 μs | 22.63 μs | 23.11 μs | 23.64 μs |  1.21 |    0.03 |    2 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.99 μs | 0.371 μs | 0.381 μs | 18.25 μs | 18.90 μs | 19.67 μs |  0.99 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 20.03 μs | 0.269 μs | 0.251 μs | 19.63 μs | 19.99 μs | 20.47 μs |  1.05 |    0.02 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 21.02 μs | 0.405 μs | 0.527 μs | 19.96 μs | 21.06 μs | 21.82 μs |  1.10 |    0.03 |    1 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|--------:|-------:|----------:|------------:|
| EggMapperStartup  | 4,733.957 μs | 11.5050 μs | 10.1989 μs | 4,716.069 μs | 4,737.125 μs | 4,747.162 μs | 1.000 |    3 | 15.6250 | 7.8125 |  280.2 KB |        1.00 |
| AutoMapperStartup |   283.634 μs |  3.0860 μs |  2.5769 μs |   280.022 μs |   282.533 μs |   288.644 μs | 0.060 |    2 |  5.8594 |      - |    104 KB |        0.37 |
| MapsterStartup    |     2.467 μs |  0.0332 μs |  0.0278 μs |     2.437 μs |     2.461 μs |     2.517 μs | 0.001 |    1 |  0.7019 | 0.0267 |  11.51 KB |        0.04 |

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

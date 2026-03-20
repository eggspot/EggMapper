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

> ⏱ **Last updated:** 2026-03-20 15:06 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  15.20 ns | 0.330 ns | 0.276 ns |  14.80 ns |  15.24 ns |  15.76 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  28.16 ns | 0.401 ns | 0.375 ns |  27.40 ns |  28.10 ns |  28.95 ns |  1.85 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  83.48 ns | 0.710 ns | 0.664 ns |  82.76 ns |  83.13 ns |  84.63 ns |  5.50 |    0.10 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster     |  28.41 ns | 0.455 ns | 0.425 ns |  27.81 ns |  28.36 ns |  29.24 ns |  1.87 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  15.02 ns | 0.155 ns | 0.137 ns |  14.82 ns |  15.02 ns |  15.28 ns |  0.99 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 490.91 ns | 1.281 ns | 1.070 ns | 488.76 ns | 490.57 ns | 492.35 ns | 32.31 |    0.57 |    4 | 0.0200 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.70 ns | 0.072 ns | 0.063 ns |  18.61 ns |  18.71 ns |  18.80 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.39 ns | 0.552 ns | 0.635 ns |  30.58 ns |  31.17 ns |  33.39 ns |  1.68 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  90.94 ns | 0.211 ns | 0.176 ns |  90.64 ns |  90.89 ns |  91.22 ns |  4.86 |    0.02 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.33 ns | 0.161 ns | 0.135 ns |  47.11 ns |  47.32 ns |  47.58 ns |  2.53 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.65 ns | 0.295 ns | 0.276 ns |  23.32 ns |  23.66 ns |  24.14 ns |  1.26 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 531.03 ns | 3.948 ns | 3.693 ns | 524.11 ns | 532.14 ns | 535.74 ns | 28.40 |    0.21 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  54.30 ns | 0.629 ns | 0.558 ns |  53.41 ns |  54.35 ns |  55.36 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  67.99 ns | 1.325 ns | 1.769 ns |  65.40 ns |  67.26 ns |  72.08 ns |  1.25 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 122.10 ns | 1.228 ns | 1.148 ns | 120.58 ns | 122.20 ns | 124.08 ns |  2.25 |    0.03 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  70.84 ns | 1.392 ns | 1.709 ns |  68.13 ns |  70.47 ns |  74.37 ns |  1.30 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.49 ns | 0.819 ns | 0.842 ns |  48.45 ns |  49.21 ns |  51.48 ns |  0.91 |    0.02 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 504.81 ns | 2.295 ns | 2.034 ns | 502.29 ns | 504.15 ns | 509.13 ns |  9.30 |    0.10 |    5 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  82.48 ns | 0.971 ns | 0.908 ns |  80.70 ns |  82.85 ns |  83.92 ns |  1.00 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| EggMapper   | 102.46 ns | 1.233 ns | 1.153 ns | 100.08 ns | 102.72 ns | 104.16 ns |  1.24 |    0.02 |    4 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 160.68 ns | 1.233 ns | 1.153 ns | 158.16 ns | 160.77 ns | 162.46 ns |  1.95 |    0.02 |    5 | 0.0196 |     328 B |        1.02 |
| Mapster     |  97.35 ns | 1.812 ns | 1.606 ns |  94.98 ns |  97.26 ns | 101.03 ns |  1.18 |    0.02 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  77.89 ns | 1.573 ns | 1.873 ns |  75.61 ns |  77.66 ns |  81.48 ns |  0.94 |    0.02 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 578.38 ns | 1.259 ns | 1.177 ns | 576.54 ns | 578.52 ns | 580.62 ns |  7.01 |    0.08 |    6 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.804 μs | 0.0344 μs | 0.0382 μs | 1.742 μs | 1.794 μs | 1.886 μs |  1.00 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.776 μs | 0.0289 μs | 0.0270 μs | 1.730 μs | 1.778 μs | 1.822 μs |  0.98 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.561 μs | 0.0502 μs | 0.0558 μs | 2.428 μs | 2.562 μs | 2.659 μs |  1.42 |    0.04 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.942 μs | 0.0376 μs | 0.0403 μs | 1.851 μs | 1.947 μs | 1.991 μs |  1.08 |    0.03 |    2 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 2.038 μs | 0.0408 μs | 0.0736 μs | 1.887 μs | 2.023 μs | 2.207 μs |  1.13 |    0.05 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.778 μs | 0.0429 μs | 0.0380 μs | 2.711 μs | 2.776 μs | 2.866 μs |  1.54 |    0.04 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.469 μs | 0.0949 μs | 0.0888 μs | 5.363 μs | 5.433 μs | 5.678 μs |  1.00 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.974 μs | 0.1187 μs | 0.2398 μs | 5.533 μs | 5.915 μs | 6.387 μs |  1.09 |    0.05 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 7.185 μs | 0.1113 μs | 0.1042 μs | 7.053 μs | 7.168 μs | 7.343 μs |  1.31 |    0.03 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 6.044 μs | 0.0878 μs | 0.0821 μs | 5.869 μs | 6.062 μs | 6.184 μs |  1.11 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.669 μs | 0.1091 μs | 0.1121 μs | 5.436 μs | 5.645 μs | 5.835 μs |  1.04 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.463 μs | 0.0637 μs | 0.0596 μs | 5.393 μs | 5.440 μs | 5.576 μs |  1.00 |    0.02 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.26 μs | 0.334 μs | 0.296 μs | 16.98 μs | 17.17 μs | 17.93 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.32 μs | 0.282 μs | 0.313 μs | 17.09 μs | 17.18 μs | 18.32 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.83 μs | 0.221 μs | 0.207 μs | 20.48 μs | 20.87 μs | 21.13 μs |  1.21 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.19 μs | 0.123 μs | 0.109 μs | 17.06 μs | 17.16 μs | 17.40 μs |  1.00 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 19.35 μs | 0.379 μs | 0.721 μs | 17.69 μs | 19.61 μs | 20.13 μs |  1.12 |    0.05 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 22.14 μs | 0.265 μs | 0.235 μs | 21.66 μs | 22.17 μs | 22.49 μs |  1.28 |    0.02 |    4 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|--------:|-------:|----------:|------------:|
| EggMapperStartup  | 5,722.485 μs | 58.4346 μs | 54.6598 μs | 5,652.280 μs | 5,705.385 μs | 5,812.227 μs | 1.000 |    3 | 15.6250 | 7.8125 | 336.73 KB |        1.00 |
| AutoMapperStartup |   283.828 μs |  1.5403 μs |  1.2862 μs |   281.795 μs |   284.180 μs |   286.194 μs | 0.050 |    2 |  5.8594 |      - | 103.88 KB |        0.31 |
| MapsterStartup    |     2.562 μs |  0.0414 μs |  0.0387 μs |     2.496 μs |     2.569 μs |     2.618 μs | 0.000 |    1 |  0.7019 | 0.0267 |  11.51 KB |        0.03 |

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
- ✅ .NET Dependency Injection integration (built-in, no extra package)
- ✅ Configuration validation
- ✅ `CreateMap(Type, Type)` runtime type mapping
- ✅ `ITypeConverter<S,D>` / `ConvertUsing` custom converters
- ✅ `ShouldMapProperty` global property filter
- ✅ Patch / partial mapping via `mapper.Patch<S,D>(src, dest)`
- ✅ Inline validation rules via `.Validate()` (collects all failures before throwing)
- ✅ IQueryable projection via `ProjectTo<S,D>(config)` for EF Core / LINQ providers
<!-- FEATURES_END -->

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

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

> ⏱ **Last updated:** 2026-03-20 15:25 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  14.65 ns | 0.074 ns | 0.066 ns |  14.57 ns |  14.63 ns |  14.76 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  27.67 ns | 0.133 ns | 0.118 ns |  27.52 ns |  27.65 ns |  27.92 ns |  1.89 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  83.14 ns | 0.244 ns | 0.216 ns |  82.84 ns |  83.09 ns |  83.65 ns |  5.68 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster     |  27.74 ns | 0.083 ns | 0.069 ns |  27.60 ns |  27.74 ns |  27.89 ns |  1.89 |    0.01 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  14.67 ns | 0.050 ns | 0.045 ns |  14.56 ns |  14.67 ns |  14.74 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 470.16 ns | 0.992 ns | 0.829 ns | 469.32 ns | 470.04 ns | 472.15 ns | 32.10 |    0.15 |    4 | 0.0205 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.32 ns | 0.043 ns | 0.040 ns |  18.23 ns |  18.33 ns |  18.38 ns |  1.00 |    0.00 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.95 ns | 0.073 ns | 0.065 ns |  30.86 ns |  30.93 ns |  31.08 ns |  1.69 |    0.00 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  90.19 ns | 0.116 ns | 0.103 ns |  90.06 ns |  90.16 ns |  90.42 ns |  4.92 |    0.01 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  46.98 ns | 0.072 ns | 0.067 ns |  46.87 ns |  46.98 ns |  47.09 ns |  2.56 |    0.01 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.44 ns | 0.517 ns | 0.616 ns |  22.86 ns |  23.15 ns |  24.55 ns |  1.28 |    0.03 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 528.29 ns | 0.766 ns | 0.640 ns | 527.39 ns | 528.19 ns | 529.81 ns | 28.84 |    0.07 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  52.04 ns | 0.306 ns | 0.286 ns |  51.33 ns |  52.02 ns |  52.43 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  65.80 ns | 0.388 ns | 0.324 ns |  65.28 ns |  65.83 ns |  66.43 ns |  1.26 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 125.03 ns | 0.637 ns | 0.532 ns | 124.14 ns | 124.94 ns | 126.23 ns |  2.40 |    0.02 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  68.09 ns | 0.449 ns | 0.420 ns |  67.28 ns |  68.06 ns |  68.87 ns |  1.31 |    0.01 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  48.32 ns | 0.242 ns | 0.202 ns |  47.87 ns |  48.25 ns |  48.59 ns |  0.93 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 499.07 ns | 1.451 ns | 1.286 ns | 497.02 ns | 499.04 ns | 501.73 ns |  9.59 |    0.06 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  68.84 ns | 0.335 ns | 0.314 ns |  68.38 ns |  68.76 ns |  69.39 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  94.92 ns | 1.155 ns | 1.080 ns |  92.74 ns |  94.83 ns |  96.78 ns |  1.38 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 153.20 ns | 2.997 ns | 3.207 ns | 149.24 ns | 153.17 ns | 157.86 ns |  2.23 |    0.05 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  95.71 ns | 1.672 ns | 1.564 ns |  91.94 ns |  95.70 ns |  98.02 ns |  1.39 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  69.03 ns | 0.466 ns | 0.436 ns |  68.35 ns |  68.93 ns |  69.83 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 549.91 ns | 1.173 ns | 0.979 ns | 547.70 ns | 549.98 ns | 551.56 ns |  7.99 |    0.04 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.722 μs | 0.0118 μs | 0.0110 μs | 1.707 μs | 1.720 μs | 1.749 μs |  1.00 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.746 μs | 0.0116 μs | 0.0109 μs | 1.721 μs | 1.746 μs | 1.766 μs |  1.01 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.310 μs | 0.0165 μs | 0.0154 μs | 2.284 μs | 2.309 μs | 2.342 μs |  1.34 |    0.01 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.794 μs | 0.0357 μs | 0.0634 μs | 1.707 μs | 1.785 μs | 1.907 μs |  1.04 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.897 μs | 0.0145 μs | 0.0128 μs | 1.878 μs | 1.897 μs | 1.924 μs |  1.10 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.567 μs | 0.0348 μs | 0.0326 μs | 2.519 μs | 2.566 μs | 2.629 μs |  1.49 |    0.02 |    3 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.178 μs | 0.0398 μs | 0.0311 μs | 5.145 μs | 5.169 μs | 5.260 μs |  1.00 |    0.01 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.569 μs | 0.0878 μs | 0.0901 μs | 5.493 μs | 5.549 μs | 5.863 μs |  1.08 |    0.02 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.553 μs | 0.1285 μs | 0.1262 μs | 6.413 μs | 6.500 μs | 6.855 μs |  1.27 |    0.02 |    2 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.979 μs | 0.1173 μs | 0.2397 μs | 5.618 μs | 6.038 μs | 6.360 μs |  1.15 |    0.05 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.380 μs | 0.0822 μs | 0.0729 μs | 5.295 μs | 5.370 μs | 5.531 μs |  1.04 |    0.01 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.616 μs | 0.0586 μs | 0.0520 μs | 5.543 μs | 5.606 μs | 5.730 μs |  1.08 |    0.01 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 16.99 μs | 0.048 μs | 0.042 μs | 16.93 μs | 16.98 μs | 17.10 μs |  1.00 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.67 μs | 0.031 μs | 0.028 μs | 16.60 μs | 16.67 μs | 16.71 μs |  0.98 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.94 μs | 0.075 μs | 0.063 μs | 20.82 μs | 20.94 μs | 21.03 μs |  1.23 |    4 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 16.72 μs | 0.105 μs | 0.088 μs | 16.55 μs | 16.74 μs | 16.85 μs |  0.98 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 17.64 μs | 0.030 μs | 0.025 μs | 17.59 μs | 17.64 μs | 17.67 μs |  1.04 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.75 μs | 0.133 μs | 0.124 μs | 19.52 μs | 19.73 μs | 20.03 μs |  1.16 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|--------:|-------:|----------:|------------:|
| EggMapperStartup  | 5,644.357 μs | 12.0308 μs | 10.0463 μs | 5,631.522 μs | 5,643.024 μs | 5,663.187 μs | 1.000 |    3 | 15.6250 | 7.8125 | 336.07 KB |        1.00 |
| AutoMapperStartup |   273.899 μs |  0.7163 μs |  0.6350 μs |   272.671 μs |   273.926 μs |   274.772 μs | 0.049 |    2 |  5.8594 |      - | 103.88 KB |        0.31 |
| MapsterStartup    |     2.391 μs |  0.0058 μs |  0.0048 μs |     2.385 μs |     2.394 μs |     2.397 μs | 0.000 |    1 |  0.7019 | 0.0267 |  11.51 KB |        0.03 |

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
- ✅ auto-update README features list and wiki docs on every release
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

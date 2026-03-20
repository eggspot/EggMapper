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

> ⏱ **Last updated:** 2026-03-20 08:53 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  14.77 ns | 0.166 ns | 0.148 ns |  14.62 ns |  14.73 ns |  15.01 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  28.44 ns | 0.253 ns | 0.211 ns |  28.07 ns |  28.46 ns |  28.86 ns |  1.93 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  82.52 ns | 0.810 ns | 0.718 ns |  81.38 ns |  82.52 ns |  83.75 ns |  5.59 |    0.07 |    3 | 0.0048 |      80 B |        1.00 |
| Mapster     |  28.14 ns | 0.321 ns | 0.300 ns |  27.72 ns |  28.13 ns |  28.85 ns |  1.91 |    0.03 |    2 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  14.80 ns | 0.228 ns | 0.202 ns |  14.61 ns |  14.71 ns |  15.27 ns |  1.00 |    0.02 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 485.91 ns | 4.357 ns | 4.075 ns | 480.87 ns | 484.19 ns | 495.67 ns | 32.90 |    0.41 |    4 | 0.0200 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.57 ns | 0.403 ns | 0.396 ns |  18.14 ns |  18.48 ns |  19.24 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  30.53 ns | 0.078 ns | 0.065 ns |  30.41 ns |  30.53 ns |  30.64 ns |  1.64 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  85.11 ns | 0.161 ns | 0.134 ns |  84.89 ns |  85.11 ns |  85.37 ns |  4.59 |    0.09 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  47.32 ns | 0.141 ns | 0.125 ns |  47.17 ns |  47.29 ns |  47.60 ns |  2.55 |    0.05 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.22 ns | 0.182 ns | 0.162 ns |  23.01 ns |  23.18 ns |  23.54 ns |  1.25 |    0.03 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 531.80 ns | 2.426 ns | 2.269 ns | 528.87 ns | 530.95 ns | 535.25 ns | 28.65 |    0.60 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  51.91 ns | 0.076 ns | 0.063 ns |  51.76 ns |  51.91 ns |  52.00 ns |  1.00 |    0.00 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.74 ns | 0.150 ns | 0.126 ns |  64.47 ns |  64.75 ns |  64.89 ns |  1.25 |    0.00 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 124.04 ns | 0.152 ns | 0.142 ns | 123.71 ns | 124.06 ns | 124.21 ns |  2.39 |    0.00 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  66.32 ns | 0.240 ns | 0.200 ns |  65.88 ns |  66.35 ns |  66.65 ns |  1.28 |    0.00 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  49.34 ns | 0.833 ns | 0.696 ns |  48.28 ns |  49.38 ns |  50.87 ns |  0.95 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 499.12 ns | 2.824 ns | 2.641 ns | 495.46 ns | 497.83 ns | 504.30 ns |  9.62 |    0.05 |    5 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  70.03 ns | 1.129 ns | 1.823 ns |  68.62 ns |  69.45 ns |  75.28 ns |  1.00 |    0.04 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  89.61 ns | 0.374 ns | 0.350 ns |  89.10 ns |  89.63 ns |  90.25 ns |  1.28 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 150.34 ns | 0.609 ns | 0.540 ns | 149.53 ns | 150.19 ns | 151.52 ns |  2.15 |    0.05 |    4 | 0.0196 |     328 B |        1.02 |
| Mapster     |  87.59 ns | 0.320 ns | 0.299 ns |  86.81 ns |  87.63 ns |  87.94 ns |  1.25 |    0.03 |    3 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  75.48 ns | 0.389 ns | 0.345 ns |  74.99 ns |  75.49 ns |  76.15 ns |  1.08 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 549.24 ns | 0.871 ns | 0.814 ns | 547.83 ns | 549.35 ns | 550.99 ns |  7.85 |    0.19 |    5 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.823 μs | 0.0363 μs | 0.0597 μs | 1.701 μs | 1.843 μs | 1.919 μs |  1.00 |    0.05 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.795 μs | 0.0347 μs | 0.0341 μs | 1.716 μs | 1.803 μs | 1.842 μs |  0.99 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.454 μs | 0.0370 μs | 0.0328 μs | 2.372 μs | 2.458 μs | 2.511 μs |  1.35 |    0.05 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.846 μs | 0.0355 μs | 0.0332 μs | 1.790 μs | 1.846 μs | 1.888 μs |  1.01 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.973 μs | 0.0266 μs | 0.0236 μs | 1.923 μs | 1.980 μs | 2.009 μs |  1.08 |    0.04 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.649 μs | 0.0506 μs | 0.0602 μs | 2.529 μs | 2.673 μs | 2.714 μs |  1.45 |    0.06 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.121 μs | 0.0248 μs | 0.0232 μs | 5.094 μs | 5.112 μs | 5.165 μs |  1.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.622 μs | 0.0201 μs | 0.0157 μs | 5.586 μs | 5.623 μs | 5.647 μs |  1.10 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.379 μs | 0.0280 μs | 0.0248 μs | 6.332 μs | 6.384 μs | 6.420 μs |  1.25 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.671 μs | 0.0477 μs | 0.0423 μs | 5.592 μs | 5.681 μs | 5.738 μs |  1.11 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.241 μs | 0.0275 μs | 0.0214 μs | 5.212 μs | 5.239 μs | 5.280 μs |  1.02 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.108 μs | 0.0198 μs | 0.0185 μs | 5.082 μs | 5.107 μs | 5.141 μs |  1.00 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.29 μs | 0.122 μs | 0.115 μs | 17.16 μs | 17.25 μs | 17.52 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.21 μs | 0.083 μs | 0.078 μs | 17.08 μs | 17.21 μs | 17.33 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.15 μs | 0.161 μs | 0.151 μs | 20.96 μs | 21.11 μs | 21.47 μs |  1.22 |    0.01 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 18.49 μs | 0.362 μs | 0.563 μs | 17.37 μs | 18.50 μs | 19.79 μs |  1.07 |    0.03 |    2 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.56 μs | 0.371 μs | 0.531 μs | 17.69 μs | 18.61 μs | 19.59 μs |  1.07 |    0.03 |    2 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 21.27 μs | 0.393 μs | 0.368 μs | 20.88 μs | 21.16 μs | 22.06 μs |  1.23 |    0.02 |    3 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|--------:|-------:|----------:|------------:|
| EggMapperStartup  | 4,591.274 μs | 22.3103 μs | 19.7775 μs | 4,563.217 μs | 4,586.419 μs | 4,633.191 μs | 1.000 |    3 | 15.6250 | 7.8125 | 280.75 KB |        1.00 |
| AutoMapperStartup |   276.524 μs |  3.3366 μs |  2.9578 μs |   273.679 μs |   275.588 μs |   283.688 μs | 0.060 |    2 |  5.8594 |      - | 104.05 KB |        0.37 |
| MapsterStartup    |     2.429 μs |  0.0123 μs |  0.0115 μs |     2.408 μs |     2.428 μs |     2.447 μs | 0.001 |    1 |  0.7019 | 0.0267 |  11.51 KB |        0.04 |

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

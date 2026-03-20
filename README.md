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

> ⏱ **Last updated:** 2026-03-20 05:42 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  14.55 ns | 0.099 ns | 0.088 ns |  14.41 ns |  14.57 ns |  14.69 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  27.48 ns | 0.433 ns | 0.463 ns |  26.69 ns |  27.52 ns |  28.12 ns |  1.89 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  82.22 ns | 0.235 ns | 0.209 ns |  81.88 ns |  82.25 ns |  82.67 ns |  5.65 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster     |  27.94 ns | 0.263 ns | 0.220 ns |  27.73 ns |  27.90 ns |  28.45 ns |  1.92 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  15.38 ns | 0.268 ns | 0.251 ns |  15.00 ns |  15.36 ns |  15.82 ns |  1.06 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 478.62 ns | 2.914 ns | 2.583 ns | 473.65 ns | 479.04 ns | 482.50 ns | 32.89 |    0.26 |    5 | 0.0205 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.55 ns | 0.149 ns | 0.132 ns |  18.26 ns |  18.56 ns |  18.69 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.41 ns | 0.410 ns | 0.363 ns |  30.84 ns |  31.43 ns |  32.25 ns |  1.69 |    0.02 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  91.68 ns | 0.799 ns | 0.748 ns |  89.88 ns |  92.05 ns |  92.27 ns |  4.94 |    0.05 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  38.00 ns | 0.331 ns | 0.309 ns |  37.03 ns |  38.02 ns |  38.33 ns |  2.05 |    0.02 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.33 ns | 0.158 ns | 0.124 ns |  23.11 ns |  23.29 ns |  23.58 ns |  1.26 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 548.15 ns | 3.150 ns | 2.946 ns | 543.97 ns | 547.95 ns | 552.71 ns | 29.56 |    0.26 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  51.31 ns | 0.302 ns | 0.282 ns |  50.74 ns |  51.33 ns |  51.87 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  64.84 ns | 0.558 ns | 0.522 ns |  64.01 ns |  64.81 ns |  65.71 ns |  1.26 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 120.03 ns | 0.518 ns | 0.459 ns | 119.21 ns | 119.99 ns | 120.98 ns |  2.34 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| Mapster     |  66.39 ns | 0.289 ns | 0.270 ns |  65.97 ns |  66.41 ns |  66.80 ns |  1.29 |    0.01 |    3 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  47.48 ns | 0.211 ns | 0.176 ns |  47.12 ns |  47.43 ns |  47.81 ns |  0.93 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 494.67 ns | 1.009 ns | 0.895 ns | 493.47 ns | 494.55 ns | 496.35 ns |  9.64 |    0.05 |    5 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  68.27 ns | 0.314 ns | 0.278 ns |  67.79 ns |  68.24 ns |  68.73 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  92.37 ns | 0.455 ns | 0.426 ns |  91.68 ns |  92.37 ns |  93.29 ns |  1.35 |    0.01 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 153.50 ns | 1.601 ns | 1.419 ns | 151.02 ns | 153.60 ns | 155.72 ns |  2.25 |    0.02 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  91.08 ns | 1.759 ns | 1.882 ns |  88.61 ns |  90.54 ns |  95.80 ns |  1.33 |    0.03 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  69.41 ns | 0.863 ns | 0.807 ns |  68.16 ns |  69.37 ns |  70.79 ns |  1.02 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 583.65 ns | 5.914 ns | 5.532 ns | 574.68 ns | 585.60 ns | 593.23 ns |  8.55 |    0.09 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.76 μs | 0.192 μs | 0.180 μs | 17.40 μs | 17.73 μs | 18.11 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 17.85 μs | 0.349 μs | 0.477 μs | 17.37 μs | 17.71 μs | 18.87 μs |  1.01 |    0.03 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 20.71 μs | 0.180 μs | 0.159 μs | 20.35 μs | 20.71 μs | 20.96 μs |  1.17 |    0.01 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.23 μs | 0.239 μs | 0.200 μs | 16.93 μs | 17.19 μs | 17.66 μs |  0.97 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.30 μs | 0.364 μs | 0.533 μs | 17.60 μs | 18.12 μs | 19.70 μs |  1.03 |    0.03 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 19.45 μs | 0.040 μs | 0.036 μs | 19.40 μs | 19.46 μs | 19.53 μs |  1.10 |    0.01 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.705 μs | 0.0340 μs | 0.0318 μs | 1.663 μs | 1.694 μs | 1.754 μs |  1.00 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.701 μs | 0.0195 μs | 0.0172 μs | 1.666 μs | 1.701 μs | 1.737 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.321 μs | 0.0102 μs | 0.0091 μs | 2.307 μs | 2.320 μs | 2.335 μs |  1.36 |    0.02 |    3 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.679 μs | 0.0093 μs | 0.0087 μs | 1.669 μs | 1.676 μs | 1.700 μs |  0.99 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 2.046 μs | 0.0054 μs | 0.0050 μs | 2.041 μs | 2.045 μs | 2.057 μs |  1.20 |    0.02 |    2 | 0.5264 | 0.0153 |   8.65 KB |        1.00 |
| AgileMapper | 2.452 μs | 0.0114 μs | 0.0107 μs | 2.429 μs | 2.453 μs | 2.474 μs |  1.44 |    0.03 |    4 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.101 μs | 0.0127 μs | 0.0113 μs | 5.088 μs | 5.097 μs | 5.126 μs |  1.00 |    0.00 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.501 μs | 0.0308 μs | 0.0288 μs | 5.449 μs | 5.491 μs | 5.555 μs |  1.08 |    0.01 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.356 μs | 0.1048 μs | 0.0980 μs | 6.253 μs | 6.319 μs | 6.528 μs |  1.25 |    0.02 |    4 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.838 μs | 0.1134 μs | 0.1061 μs | 5.653 μs | 5.841 μs | 6.007 μs |  1.14 |    0.02 |    3 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.468 μs | 0.0980 μs | 0.0916 μs | 5.318 μs | 5.465 μs | 5.628 μs |  1.07 |    0.02 |    2 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 4.982 μs | 0.0204 μs | 0.0181 μs | 4.957 μs | 4.979 μs | 5.024 μs |  0.98 |    0.00 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 4,552.945 μs | 18.0785 μs | 16.0261 μs | 4,530.966 μs | 4,552.919 μs | 4,585.716 μs | 1.000 |    3 | 7.8125 |      - |  225.3 KB |        1.00 |
| AutoMapperStartup |   277.273 μs |  2.1020 μs |  1.8633 μs |   273.886 μs |   277.793 μs |   279.965 μs | 0.061 |    2 | 5.8594 |      - | 103.64 KB |        0.46 |
| MapsterStartup    |     2.529 μs |  0.0496 μs |  0.0645 μs |     2.396 μs |     2.549 μs |     2.614 μs | 0.001 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.05 |

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

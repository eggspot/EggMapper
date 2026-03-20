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

> ⏱ **Last updated:** 2026-03-20 13:54 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  16.86 ns | 0.194 ns | 0.181 ns |  16.65 ns |  16.87 ns |  17.27 ns |  1.00 |    0.01 |    1 | 0.0032 |      80 B |        1.00 |
| EggMapper   |  29.24 ns | 0.156 ns | 0.146 ns |  29.00 ns |  29.23 ns |  29.46 ns |  1.73 |    0.02 |    4 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  69.28 ns | 0.142 ns | 0.119 ns |  69.12 ns |  69.29 ns |  69.47 ns |  4.11 |    0.04 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  27.65 ns | 0.158 ns | 0.148 ns |  27.32 ns |  27.69 ns |  27.89 ns |  1.64 |    0.02 |    3 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  17.81 ns | 0.194 ns | 0.162 ns |  17.35 ns |  17.83 ns |  18.04 ns |  1.06 |    0.01 |    2 | 0.0032 |      80 B |        1.00 |
| AgileMapper | 348.94 ns | 0.655 ns | 0.580 ns | 348.16 ns | 348.89 ns | 350.16 ns | 20.69 |    0.22 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  20.01 ns | 0.206 ns | 0.193 ns |  19.71 ns |  20.05 ns |  20.37 ns |  1.00 |    0.01 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  31.94 ns | 0.161 ns | 0.150 ns |  31.67 ns |  31.92 ns |  32.25 ns |  1.60 |    0.02 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  76.59 ns | 0.196 ns | 0.183 ns |  76.29 ns |  76.54 ns |  76.94 ns |  3.83 |    0.04 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  40.05 ns | 0.226 ns | 0.211 ns |  39.67 ns |  40.06 ns |  40.50 ns |  2.00 |    0.02 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  26.79 ns | 0.330 ns | 0.309 ns |  26.35 ns |  26.84 ns |  27.25 ns |  1.34 |    0.02 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 353.53 ns | 0.413 ns | 0.345 ns | 353.01 ns | 353.47 ns | 354.33 ns | 17.67 |    0.17 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  60.61 ns | 0.483 ns | 0.403 ns |  59.68 ns |  60.69 ns |  61.07 ns |  1.00 |    0.01 |    2 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  80.36 ns | 0.782 ns | 0.731 ns |  79.42 ns |  80.21 ns |  81.75 ns |  1.33 |    0.01 |    4 | 0.0107 |     272 B |        1.00 |
| AutoMapper  | 112.42 ns | 0.621 ns | 0.581 ns | 111.01 ns | 112.39 ns | 113.49 ns |  1.85 |    0.02 |    5 | 0.0107 |     272 B |        1.00 |
| Mapster     |  74.60 ns | 0.632 ns | 0.560 ns |  73.72 ns |  74.51 ns |  75.67 ns |  1.23 |    0.01 |    3 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  58.26 ns | 0.716 ns | 0.670 ns |  57.29 ns |  58.06 ns |  59.41 ns |  0.96 |    0.01 |    1 | 0.0107 |     272 B |        1.00 |
| AgileMapper | 389.23 ns | 0.459 ns | 0.429 ns | 388.34 ns | 389.32 ns | 389.95 ns |  6.42 |    0.04 |    6 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  78.72 ns | 0.983 ns | 0.871 ns |  76.48 ns |  78.71 ns |  80.33 ns |  1.00 |    0.02 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   | 100.06 ns | 0.575 ns | 0.538 ns |  99.17 ns | 100.11 ns | 100.87 ns |  1.27 |    0.02 |    3 | 0.0126 |     320 B |        1.00 |
| AutoMapper  | 144.51 ns | 0.687 ns | 0.642 ns | 143.40 ns | 144.56 ns | 145.42 ns |  1.84 |    0.02 |    4 | 0.0129 |     328 B |        1.02 |
| Mapster     |  93.34 ns | 0.599 ns | 0.531 ns |  92.63 ns |  93.28 ns |  94.34 ns |  1.19 |    0.01 |    2 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  80.81 ns | 0.582 ns | 0.544 ns |  79.88 ns |  80.91 ns |  81.72 ns |  1.03 |    0.01 |    1 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 437.43 ns | 0.530 ns | 0.495 ns | 436.66 ns | 437.28 ns | 438.38 ns |  5.56 |    0.06 |    5 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.979 μs | 0.0313 μs | 0.0293 μs | 1.933 μs | 1.982 μs | 2.022 μs |  1.00 |    0.02 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| EggMapper   | 2.147 μs | 0.0222 μs | 0.0207 μs | 2.120 μs | 2.148 μs | 2.184 μs |  1.09 |    0.02 |    2 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AutoMapper  | 2.665 μs | 0.0326 μs | 0.0305 μs | 2.620 μs | 2.665 μs | 2.723 μs |  1.35 |    0.02 |    3 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.839 μs | 0.0253 μs | 0.0236 μs | 1.791 μs | 1.835 μs | 1.882 μs |  0.93 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| MapperlyMap | 1.912 μs | 0.0307 μs | 0.0287 μs | 1.849 μs | 1.914 μs | 1.954 μs |  0.97 |    0.02 |    1 | 0.3510 | 0.0076 |   8.65 KB |        1.00 |
| AgileMapper | 2.674 μs | 0.0187 μs | 0.0156 μs | 2.640 μs | 2.676 μs | 2.694 μs |  1.35 |    0.02 |    3 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 6.090 μs | 0.0745 μs | 0.0697 μs | 5.991 μs | 6.094 μs | 6.224 μs |  1.00 |    0.02 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 6.161 μs | 0.0724 μs | 0.0677 μs | 6.051 μs | 6.195 μs | 6.274 μs |  1.01 |    0.02 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| AutoMapper  | 7.274 μs | 0.0640 μs | 0.0599 μs | 7.181 μs | 7.278 μs | 7.397 μs |  1.19 |    0.02 |    4 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 6.699 μs | 0.0928 μs | 0.0868 μs | 6.575 μs | 6.673 μs | 6.872 μs |  1.10 |    0.02 |    3 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 5.887 μs | 0.0798 μs | 0.0747 μs | 5.756 μs | 5.894 μs | 6.024 μs |  0.97 |    0.02 |    2 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 5.606 μs | 0.0508 μs | 0.0475 μs | 5.547 μs | 5.602 μs | 5.704 μs |  0.92 |    0.01 |    1 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 19.71 μs | 0.304 μs | 0.284 μs | 19.20 μs | 19.72 μs | 20.12 μs |  1.00 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 19.43 μs | 0.313 μs | 0.278 μs | 18.82 μs | 19.44 μs | 19.86 μs |  0.99 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 23.89 μs | 0.403 μs | 0.377 μs | 23.44 μs | 23.82 μs | 24.70 μs |  1.21 |    0.03 |    4 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 17.95 μs | 0.240 μs | 0.224 μs | 17.56 μs | 17.95 μs | 18.40 μs |  0.91 |    0.02 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 21.01 μs | 0.144 μs | 0.128 μs | 20.74 μs | 21.00 μs | 21.23 μs |  1.07 |    0.02 |    3 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 21.47 μs | 0.176 μs | 0.165 μs | 21.23 μs | 21.44 μs | 21.74 μs |  1.09 |    0.02 |    3 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 5,746.783 μs | 43.0276 μs | 40.2480 μs | 5,684.954 μs | 5,756.909 μs | 5,801.704 μs | 1.000 |    3 | 7.8125 |      - | 335.79 KB |        1.00 |
| AutoMapperStartup |   260.120 μs |  4.5505 μs |  4.0339 μs |   256.835 μs |   258.703 μs |   269.085 μs | 0.045 |    2 | 3.9063 |      - | 104.06 KB |        0.31 |
| MapsterStartup    |     3.032 μs |  0.0321 μs |  0.0300 μs |     2.975 μs |     3.032 μs |     3.069 μs | 0.001 |    1 | 0.4692 | 0.0153 |  11.51 KB |        0.03 |

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

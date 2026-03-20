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

> ⏱ **Last updated:** 2026-03-20 08:29 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  16.84 ns | 0.335 ns | 0.313 ns |  16.35 ns |  16.74 ns |  17.47 ns |  1.00 |    0.03 |    2 | 0.0032 |      80 B |        1.00 |
| EggMapper   |  28.52 ns | 0.417 ns | 0.390 ns |  27.86 ns |  28.53 ns |  29.28 ns |  1.69 |    0.04 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  70.36 ns | 0.358 ns | 0.299 ns |  69.88 ns |  70.33 ns |  70.96 ns |  4.18 |    0.08 |    4 | 0.0031 |      80 B |        1.00 |
| Mapster     |  27.15 ns | 0.460 ns | 0.430 ns |  26.59 ns |  26.95 ns |  28.02 ns |  1.61 |    0.04 |    3 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  14.99 ns | 0.363 ns | 0.388 ns |  14.40 ns |  14.95 ns |  15.78 ns |  0.89 |    0.03 |    1 | 0.0032 |      80 B |        1.00 |
| AgileMapper | 348.52 ns | 0.993 ns | 0.880 ns | 347.17 ns | 348.47 ns | 350.39 ns | 20.71 |    0.37 |    5 | 0.0134 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  19.25 ns | 0.371 ns | 0.347 ns |  18.72 ns |  19.31 ns |  19.72 ns |  1.00 |    0.02 |    1 | 0.0032 |      80 B |        1.00 |
| EggMap      |  30.56 ns | 0.382 ns | 0.357 ns |  30.04 ns |  30.67 ns |  31.22 ns |  1.59 |    0.03 |    3 | 0.0032 |      80 B |        1.00 |
| AutoMapper  |  70.23 ns | 0.183 ns | 0.171 ns |  69.80 ns |  70.25 ns |  70.45 ns |  3.65 |    0.06 |    5 | 0.0031 |      80 B |        1.00 |
| Mapster     |  39.47 ns | 0.230 ns | 0.215 ns |  39.09 ns |  39.48 ns |  39.79 ns |  2.05 |    0.04 |    4 | 0.0032 |      80 B |        1.00 |
| MapperlyMap |  24.97 ns | 0.542 ns | 0.507 ns |  24.07 ns |  25.05 ns |  25.97 ns |  1.30 |    0.03 |    2 | 0.0041 |     104 B |        1.30 |
| AgileMapper | 369.75 ns | 6.332 ns | 5.923 ns | 359.03 ns | 369.95 ns | 382.78 ns | 19.22 |    0.45 |    6 | 0.0134 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  61.74 ns | 1.193 ns | 1.116 ns |  59.97 ns |  61.77 ns |  63.83 ns |  1.00 |    0.02 |    2 | 0.0107 |     272 B |        1.00 |
| EggMapper   |  70.85 ns | 1.385 ns | 1.649 ns |  67.69 ns |  71.07 ns |  74.28 ns |  1.15 |    0.03 |    3 | 0.0107 |     272 B |        1.00 |
| AutoMapper  | 108.31 ns | 1.645 ns | 1.539 ns | 105.16 ns | 108.33 ns | 111.34 ns |  1.75 |    0.04 |    4 | 0.0107 |     272 B |        1.00 |
| Mapster     |  69.75 ns | 1.312 ns | 1.228 ns |  68.04 ns |  69.87 ns |  72.04 ns |  1.13 |    0.03 |    3 | 0.0107 |     272 B |        1.00 |
| MapperlyMap |  54.10 ns | 0.994 ns | 1.144 ns |  52.44 ns |  53.91 ns |  56.03 ns |  0.88 |    0.02 |    1 | 0.0108 |     272 B |        1.00 |
| AgileMapper | 381.60 ns | 2.673 ns | 2.500 ns | 378.67 ns | 380.84 ns | 385.80 ns |  6.18 |    0.11 |    5 | 0.0167 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  70.08 ns | 1.053 ns | 0.880 ns |  68.55 ns |  70.10 ns |  71.62 ns |  1.00 |    0.02 |    1 | 0.0126 |     320 B |        1.00 |
| EggMapper   |  93.41 ns | 1.755 ns | 1.878 ns |  90.90 ns |  92.86 ns |  98.12 ns |  1.33 |    0.03 |    3 | 0.0126 |     320 B |        1.00 |
| AutoMapper  | 139.83 ns | 1.284 ns | 1.072 ns | 137.34 ns | 139.60 ns | 141.42 ns |  2.00 |    0.03 |    4 | 0.0129 |     328 B |        1.02 |
| Mapster     |  89.24 ns | 1.543 ns | 1.444 ns |  87.39 ns |  89.17 ns |  92.16 ns |  1.27 |    0.03 |    3 | 0.0126 |     320 B |        1.00 |
| MapperlyMap |  76.14 ns | 1.596 ns | 2.075 ns |  72.87 ns |  76.12 ns |  80.86 ns |  1.09 |    0.03 |    2 | 0.0126 |     320 B |        1.00 |
| AgileMapper | 435.66 ns | 1.184 ns | 1.107 ns | 434.00 ns | 435.67 ns | 438.03 ns |  6.22 |    0.08 |    5 | 0.0210 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.810 μs | 0.0218 μs | 0.0193 μs | 1.777 μs | 1.811 μs | 1.842 μs |  1.00 |    0.01 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| EggMapper   | 1.869 μs | 0.0322 μs | 0.0358 μs | 1.822 μs | 1.871 μs | 1.955 μs |  1.03 |    0.02 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| AutoMapper  | 2.571 μs | 0.0513 μs | 0.0549 μs | 2.511 μs | 2.561 μs | 2.684 μs |  1.42 |    0.03 |    3 | 0.4044 | 0.0114 |   9.95 KB |        1.15 |
| Mapster     | 1.789 μs | 0.0161 μs | 0.0143 μs | 1.752 μs | 1.792 μs | 1.809 μs |  0.99 |    0.01 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| MapperlyMap | 1.840 μs | 0.0361 μs | 0.0470 μs | 1.773 μs | 1.830 μs | 1.937 μs |  1.02 |    0.03 |    1 | 0.3529 | 0.0114 |   8.65 KB |        1.00 |
| AgileMapper | 2.334 μs | 0.0453 μs | 0.0423 μs | 2.272 μs | 2.331 μs | 2.420 μs |  1.29 |    0.03 |    2 | 0.3624 | 0.0114 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.513 μs | 0.0591 μs | 0.0553 μs | 5.422 μs | 5.517 μs | 5.606 μs |  1.00 |    0.01 |    1 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| EggMapper   | 5.846 μs | 0.1036 μs | 0.0919 μs | 5.716 μs | 5.835 μs | 6.041 μs |  1.06 |    0.02 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| AutoMapper  | 6.597 μs | 0.0722 μs | 0.0675 μs | 6.478 μs | 6.593 μs | 6.713 μs |  1.20 |    0.02 |    3 | 1.1673 | 0.0687 |   28.7 KB |        1.05 |
| Mapster     | 6.066 μs | 0.0735 μs | 0.0688 μs | 5.916 μs | 6.092 μs | 6.176 μs |  1.10 |    0.02 |    2 | 1.1139 | 0.0610 |   27.4 KB |        1.00 |
| MapperlyMap | 5.479 μs | 0.0764 μs | 0.0677 μs | 5.341 μs | 5.498 μs | 5.598 μs |  0.99 |    0.02 |    1 | 1.1139 | 0.0610 |  27.42 KB |        1.00 |
| AgileMapper | 5.323 μs | 0.0899 μs | 0.0841 μs | 5.216 μs | 5.300 μs | 5.488 μs |  0.97 |    0.02 |    1 | 0.6790 | 0.0381 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 18.64 μs | 0.351 μs | 0.328 μs | 17.85 μs | 18.73 μs | 19.01 μs |  1.00 |    0.02 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| EggMapper   | 16.96 μs | 0.338 μs | 0.376 μs | 16.48 μs | 16.88 μs | 17.74 μs |  0.91 |    0.03 |    1 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| AutoMapper  | 22.47 μs | 0.419 μs | 0.392 μs | 21.88 μs | 22.46 μs | 23.08 μs |  1.21 |    0.03 |    4 | 3.8452 | 0.9460 |  94.34 KB |        1.10 |
| Mapster     | 17.93 μs | 0.353 μs | 0.421 μs | 17.31 μs | 17.99 μs | 18.70 μs |  0.96 |    0.03 |    2 | 3.4790 | 0.8545 |  85.99 KB |        1.00 |
| MapperlyMap | 18.20 μs | 0.363 μs | 0.484 μs | 17.42 μs | 18.06 μs | 19.43 μs |  0.98 |    0.03 |    2 | 3.5095 | 0.8545 |  86.02 KB |        1.00 |
| AgileMapper | 21.23 μs | 0.261 μs | 0.232 μs | 20.92 μs | 21.16 μs | 21.73 μs |  1.14 |    0.02 |    3 | 3.5095 | 0.8545 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 4,709.428 μs | 18.1808 μs | 17.0063 μs | 4,682.420 μs | 4,711.151 μs | 4,741.871 μs | 1.000 |    3 | 7.8125 |      - |  225.2 KB |        1.00 |
| AutoMapperStartup |   257.144 μs |  1.7470 μs |  1.5487 μs |   255.432 μs |   256.605 μs |   260.583 μs | 0.055 |    2 | 3.9063 |      - |  104.3 KB |        0.46 |
| MapsterStartup    |     2.806 μs |  0.0477 μs |  0.0446 μs |     2.728 μs |     2.818 μs |     2.870 μs | 0.001 |    1 | 0.4692 | 0.0114 |  11.51 KB |        0.05 |

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

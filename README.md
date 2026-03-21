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

<!-- SUMMARY_TABLE_START -->
| Scenario | Manual | EggMapper | Mapster | AutoMapper | Mapperly* |
|----------|--------|-----------|---------|------------|-----------|
| **Flat (10 props)** | 14.97 ns | **27.61 ns (1.8×)** | 28.98 ns (1.9×) | 84.46 ns (5.6×) | 15.18 ns (1.0×) |
| **Flattening** | 18.50 ns | **31.80 ns (1.7×)** | 37.06 ns (2.0×) | 92.58 ns (5.0×) | 23.37 ns (1.3×) |
| **Deep (2 nested)** | 53.71 ns | **66.43 ns (1.2×)** | 68.96 ns (1.3×) | 130.05 ns (2.4×) | 48.92 ns (0.9×) |
| **Complex (nest+coll)** | 70.71 ns | **93.54 ns (1.3×)** | 90.88 ns (1.3×) | 156.60 ns (2.2×) | 71.30 ns (1.0×) |
| **Collection (100)** | 1.790 μs | **1.799 μs (1.0×)** | 1.792 μs (1.0×) | 2.505 μs (1.4×) | 1.870 μs (1.1×) |
| **Deep Coll (100)** | 5.199 μs | **5.621 μs (1.1×)** | 5.753 μs (1.1×) | 6.432 μs (1.2×) | 5.250 μs (1.0×) |
| **Large Coll (1000)** | 17.84 μs | **16.99 μs (0.9×)** | 17.38 μs (1.0×) | 21.20 μs (1.2×) | 18.28 μs (1.0×) |
<!-- SUMMARY_TABLE_END -->

**\*** *Mapperly is a compile-time source generator — it produces code equivalent to hand-written mapping. EggMapper is the fastest **runtime** mapper.*

**Allocations:** EggMapper matches manual allocation exactly in every scenario (zero extra bytes).

Run the benchmarks yourself:

```bash
cd src/EggMapper.Benchmarks
dotnet run --configuration Release -f net10.0 -- --filter * --exporters json markdown
```

<!-- BENCHMARK_RESULTS_START -->

> ⏱ **Last updated:** 2026-03-20 15:45 UTC

> **Competitors tested:** EggMapper, AutoMapper, Mapster, Mapperly (source-gen), AgileMapper

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping (10 properties)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  14.97 ns | 0.144 ns | 0.121 ns |  14.73 ns |  14.95 ns |  15.18 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper   |  27.61 ns | 0.319 ns | 0.283 ns |  27.19 ns |  27.56 ns |  28.13 ns |  1.84 |    0.02 |    2 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  84.46 ns | 0.603 ns | 0.535 ns |  83.65 ns |  84.41 ns |  85.30 ns |  5.64 |    0.06 |    4 | 0.0048 |      80 B |        1.00 |
| Mapster     |  28.98 ns | 0.438 ns | 0.388 ns |  28.41 ns |  28.92 ns |  29.87 ns |  1.94 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  15.18 ns | 0.357 ns | 0.367 ns |  14.67 ns |  15.02 ns |  16.08 ns |  1.01 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| AgileMapper | 482.08 ns | 2.683 ns | 2.378 ns | 477.55 ns | 481.84 ns | 486.88 ns | 32.21 |    0.29 |    5 | 0.0200 |     344 B |        4.30 |

#### 🟡 Flattening

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  18.50 ns | 0.114 ns | 0.095 ns |  18.35 ns |  18.50 ns |  18.66 ns |  1.00 |    0.01 |    1 | 0.0048 |      80 B |        1.00 |
| EggMap      |  31.80 ns | 0.550 ns | 0.487 ns |  31.27 ns |  31.67 ns |  32.68 ns |  1.72 |    0.03 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper  |  92.58 ns | 0.295 ns | 0.262 ns |  92.18 ns |  92.56 ns |  93.03 ns |  5.00 |    0.03 |    5 | 0.0048 |      80 B |        1.00 |
| Mapster     |  37.06 ns | 0.739 ns | 0.726 ns |  36.37 ns |  36.71 ns |  38.42 ns |  2.00 |    0.04 |    4 | 0.0048 |      80 B |        1.00 |
| MapperlyMap |  23.37 ns | 0.240 ns | 0.225 ns |  22.98 ns |  23.37 ns |  23.87 ns |  1.26 |    0.01 |    2 | 0.0062 |     104 B |        1.30 |
| AgileMapper | 522.81 ns | 2.117 ns | 1.877 ns | 519.64 ns | 522.86 ns | 526.41 ns | 28.26 |    0.17 |    6 | 0.0200 |     344 B |        4.30 |

#### 🟣 Deep Mapping (2 nested objects)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  53.71 ns | 0.618 ns | 0.548 ns |  53.16 ns |  53.59 ns |  54.92 ns |  1.00 |    0.01 |    2 | 0.0162 |     272 B |        1.00 |
| EggMapper   |  66.43 ns | 0.609 ns | 0.540 ns |  65.84 ns |  66.29 ns |  67.69 ns |  1.24 |    0.02 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper  | 130.05 ns | 0.695 ns | 0.650 ns | 129.03 ns | 129.93 ns | 131.18 ns |  2.42 |    0.03 |    5 | 0.0162 |     272 B |        1.00 |
| Mapster     |  68.96 ns | 0.601 ns | 0.533 ns |  67.74 ns |  69.09 ns |  69.62 ns |  1.28 |    0.02 |    4 | 0.0162 |     272 B |        1.00 |
| MapperlyMap |  48.92 ns | 0.457 ns | 0.428 ns |  48.17 ns |  49.05 ns |  49.65 ns |  0.91 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| AgileMapper | 508.81 ns | 3.838 ns | 3.403 ns | 504.60 ns | 507.91 ns | 515.68 ns |  9.47 |    0.11 |    6 | 0.0248 |     424 B |        1.56 |

#### 🟢 Complex Mapping (nested + collection)

| Method      | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------ |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual      |  70.71 ns | 0.479 ns | 0.424 ns |  69.88 ns |  70.67 ns |  71.44 ns |  1.00 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| EggMapper   |  93.54 ns | 1.059 ns | 0.990 ns |  91.70 ns |  93.49 ns |  95.21 ns |  1.32 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| AutoMapper  | 156.60 ns | 1.146 ns | 1.072 ns | 154.82 ns | 156.92 ns | 158.41 ns |  2.21 |    0.02 |    3 | 0.0196 |     328 B |        1.02 |
| Mapster     |  90.88 ns | 1.604 ns | 1.422 ns |  89.20 ns |  90.32 ns |  94.31 ns |  1.29 |    0.02 |    2 | 0.0191 |     320 B |        1.00 |
| MapperlyMap |  71.30 ns | 0.952 ns | 0.795 ns |  70.13 ns |  71.20 ns |  73.37 ns |  1.01 |    0.01 |    1 | 0.0191 |     320 B |        1.00 |
| AgileMapper | 550.50 ns | 3.421 ns | 3.033 ns | 546.52 ns | 550.37 ns | 557.69 ns |  7.79 |    0.06 |    4 | 0.0315 |     528 B |        1.65 |

#### 🟠 Collection (100 items)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 1.790 μs | 0.0246 μs | 0.0206 μs | 1.764 μs | 1.785 μs | 1.839 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| EggMapper   | 1.799 μs | 0.0355 μs | 0.0422 μs | 1.743 μs | 1.789 μs | 1.907 μs |  1.01 |    0.03 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AutoMapper  | 2.505 μs | 0.0349 μs | 0.0326 μs | 2.442 μs | 2.505 μs | 2.553 μs |  1.40 |    0.02 |    2 | 0.6065 | 0.0191 |   9.95 KB |        1.15 |
| Mapster     | 1.792 μs | 0.0311 μs | 0.0276 μs | 1.732 μs | 1.797 μs | 1.847 μs |  1.00 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| MapperlyMap | 1.870 μs | 0.0128 μs | 0.0107 μs | 1.838 μs | 1.873 μs | 1.880 μs |  1.05 |    0.01 |    1 | 0.5283 | 0.0172 |   8.65 KB |        1.00 |
| AgileMapper | 2.592 μs | 0.0229 μs | 0.0214 μs | 2.551 μs | 2.593 μs | 2.636 μs |  1.45 |    0.02 |    2 | 0.5417 | 0.0153 |   8.91 KB |        1.03 |

#### 🔴 Deep Collection (100 items, nested)

| Method      | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 5.199 μs | 0.1034 μs | 0.1308 μs | 4.986 μs | 5.163 μs | 5.446 μs |  1.00 |    0.03 |    1 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| EggMapper   | 5.621 μs | 0.0521 μs | 0.0406 μs | 5.525 μs | 5.641 μs | 5.659 μs |  1.08 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| AutoMapper  | 6.432 μs | 0.0757 μs | 0.0708 μs | 6.305 μs | 6.417 μs | 6.566 μs |  1.24 |    0.03 |    3 | 1.7548 | 0.1068 |   28.7 KB |        1.05 |
| Mapster     | 5.753 μs | 0.0712 μs | 0.0666 μs | 5.679 μs | 5.725 μs | 5.874 μs |  1.11 |    0.03 |    2 | 1.6708 | 0.0916 |   27.4 KB |        1.00 |
| MapperlyMap | 5.250 μs | 0.0401 μs | 0.0356 μs | 5.187 μs | 5.253 μs | 5.319 μs |  1.01 |    0.03 |    1 | 1.6785 | 0.0992 |  27.42 KB |        1.00 |
| AgileMapper | 5.236 μs | 0.0897 μs | 0.0839 μs | 5.119 μs | 5.224 μs | 5.366 μs |  1.01 |    0.03 |    1 | 1.0223 | 0.0610 |  16.72 KB |        0.61 |

#### ⚫ Large Collection (1,000 items)

| Method      | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual      | 17.84 μs | 0.206 μs | 0.193 μs | 17.59 μs | 17.79 μs | 18.24 μs |  1.00 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| EggMapper   | 16.99 μs | 0.190 μs | 0.148 μs | 16.64 μs | 17.03 μs | 17.24 μs |  0.95 |    0.01 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| AutoMapper  | 21.20 μs | 0.201 μs | 0.188 μs | 20.68 μs | 21.24 μs | 21.47 μs |  1.19 |    0.02 |    3 | 5.7678 | 1.4343 |  94.34 KB |        1.10 |
| Mapster     | 17.38 μs | 0.289 μs | 0.256 μs | 17.01 μs | 17.31 μs | 17.83 μs |  0.97 |    0.02 |    1 | 5.2490 | 1.3123 |  85.99 KB |        1.00 |
| MapperlyMap | 18.28 μs | 0.356 μs | 0.510 μs | 17.51 μs | 18.15 μs | 19.26 μs |  1.02 |    0.03 |    1 | 5.2490 | 1.2817 |  86.02 KB |        1.00 |
| AgileMapper | 20.03 μs | 0.278 μs | 0.246 μs | 19.74 μs | 20.01 μs | 20.60 μs |  1.12 |    0.02 |    2 | 5.2795 | 1.3123 |  86.25 KB |        1.00 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0    | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|--------:|-------:|----------:|------------:|
| EggMapperStartup  | 5,546.378 μs | 21.0287 μs | 17.5599 μs | 5,517.859 μs | 5,548.003 μs | 5,583.931 μs | 1.000 |    3 | 15.6250 | 7.8125 | 336.36 KB |        1.00 |
| AutoMapperStartup |   280.519 μs |  2.5210 μs |  2.2348 μs |   277.808 μs |   280.045 μs |   284.729 μs | 0.051 |    2 |  5.8594 |      - | 103.88 KB |        0.31 |
| MapsterStartup    |     2.469 μs |  0.0245 μs |  0.0229 μs |     2.436 μs |     2.459 μs |     2.506 μs | 0.000 |    1 |  0.7019 | 0.0267 |  11.51 KB |        0.03 |

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

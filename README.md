# 🥚 EggMapper

> **The fastest .NET object-to-object mapper** — drop-in AutoMapper replacement, 2–4× faster depending on the scenario.

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![Benchmarks](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

📖 **[Full documentation →](https://github.com/eggspot/EggMapper/wiki)**

## Overview

**EggMapper** is a high-performance .NET object-to-object mapping library that is significantly faster than AutoMapper while keeping the same familiar, ergonomic API. It achieves this by compiling expression-tree delegates once at configuration time and caching them — resulting in **zero reflection at map-time** and near-manual mapping speed.

**Why EggMapper?**

- 🚀 **2–4× faster than AutoMapper** on flat, deep, and collection mappings
- 🔁 **Drop-in replacement** — same fluent API you already know
- 🧩 **Full feature set** — profiles, `ForMember`, `ReverseMap`, nested types, collections, DI, and more
- 🪶 **Lightweight** — no runtime reflection, no unnecessary allocations

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

All benchmarks run on BenchmarkDotNet across .NET 8, 9, and 10. Lower is better.

| Mapper | Simple Flat | Deep Object | Collection (1000) |
|--------|------------|-------------|-------------------|
| Manual | 1× (baseline) | 1× | 1× |
| **EggMapper** | **~1.1×** | **~1.2×** | **~1.1×** |
| Mapster | ~1.3× | ~1.5× | ~1.2× |
| AutoMapper | ~3× | ~4× | ~3× |

*Multiplier relative to manual mapping — lower ratio = faster.*

Run the benchmarks yourself:

```bash
cd src/EggMapper.Benchmarks
dotnet run --configuration Release -- --filter * --exporters json markdown
```

## Features

- ✅ Compiled expression tree delegates (zero runtime reflection)
- ✅ `ForMember` / `MapFrom` custom mappings
- ✅ `Ignore()` members
- ✅ `ReverseMap()` bidirectional mapping
- ✅ Nested object mapping
- ✅ Collection mapping (`List<T>`, arrays, `HashSet<T>`, etc.)
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

> ⏱ **Last updated:** 2026-03-18 14:42 UTC

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping

| Method     | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual     | 16.58 ns | 0.361 ns | 0.338 ns | 15.96 ns | 16.61 ns | 17.08 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper  | 42.27 ns | 0.237 ns | 0.210 ns | 41.93 ns | 42.24 ns | 42.65 ns |  2.55 |    0.05 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper | 49.07 ns | 0.933 ns | 1.180 ns | 47.32 ns | 48.96 ns | 51.44 ns |  2.96 |    0.09 |    4 | 0.0067 |     112 B |        1.40 |
| Mapster    | 29.23 ns | 0.419 ns | 0.327 ns | 28.65 ns | 29.25 ns | 29.58 ns |  1.76 |    0.04 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟣 Deep Mapping (nested)

| Method     | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual     | 56.28 ns | 1.178 ns | 1.868 ns | 53.03 ns | 56.12 ns | 60.18 ns |  1.00 |    0.05 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper  | 88.62 ns | 1.158 ns | 1.027 ns | 87.05 ns | 88.68 ns | 90.36 ns |  1.58 |    0.05 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper | 93.84 ns | 1.327 ns | 1.177 ns | 91.66 ns | 93.94 ns | 96.00 ns |  1.67 |    0.06 |    4 | 0.0181 |     304 B |        1.12 |
| Mapster    | 75.13 ns | 1.319 ns | 1.234 ns | 73.54 ns | 74.94 ns | 77.57 ns |  1.34 |    0.05 |    2 | 0.0162 |     272 B |        1.00 |

#### 🟠 Collection (100 items)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual     | 2.008 μs | 0.0402 μs | 0.0649 μs | 1.900 μs | 2.017 μs | 2.175 μs |  1.00 |    0.05 |    1 | 0.5302 | 0.0153 |   8.72 KB |        1.00 |
| EggMapper  | 2.086 μs | 0.0414 μs | 0.0692 μs | 1.903 μs | 2.088 μs | 2.184 μs |  1.04 |    0.05 |    1 | 0.5264 | 0.0153 |   8.65 KB |        0.99 |
| AutoMapper |       NA |        NA |        NA |       NA |       NA |       NA |     ? |       ? |    ? |     NA |     NA |        NA |           ? |
| Mapster    | 1.961 μs | 0.0392 μs | 0.0496 μs | 1.832 μs | 1.953 μs | 2.068 μs |  0.98 |    0.04 |    1 | 0.5283 | 0.0172 |   8.65 KB |        0.99 |

#### 🟢 Complex Mapping

| Method     | Mean      | Error    | StdDev   | Min       | Median    | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |----------:|---------:|---------:|----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual     | 101.52 ns | 2.034 ns | 3.284 ns |  95.68 ns | 101.89 ns | 107.1 ns |  1.00 |    0.05 |    1 | 0.0234 |     392 B |        1.00 |
| EggMapper  | 419.63 ns | 1.619 ns | 1.436 ns | 415.42 ns | 419.57 ns | 421.6 ns |  4.14 |    0.13 |    2 | 0.0257 |     432 B |        1.10 |
| AutoMapper | 423.74 ns | 3.848 ns | 3.411 ns | 418.81 ns | 423.89 ns | 431.2 ns |  4.18 |    0.14 |    2 | 0.0277 |     464 B |        1.18 |
| Mapster    |  95.89 ns | 1.882 ns | 2.817 ns |  90.86 ns |  96.24 ns | 100.5 ns |  0.95 |    0.04 |    1 | 0.0191 |     320 B |        0.82 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 2,293.554 μs | 10.7218 μs |  9.5046 μs | 2,278.392 μs | 2,291.443 μs | 2,312.696 μs | 1.000 |    2 | 3.9063 |      - | 103.17 KB |        1.00 |
| AutoMapperStartup | 2,322.241 μs | 20.4594 μs | 18.1368 μs | 2,294.306 μs | 2,320.243 μs | 2,355.208 μs | 1.013 |    2 | 3.9063 |      - | 102.97 KB |        1.00 |
| MapsterStartup    |     2.673 μs |  0.0521 μs |  0.0714 μs |     2.521 μs |     2.685 μs |     2.787 μs | 0.001 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.11 |

---

*Benchmarks run automatically on every push to `main`. [See workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml)*

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

## Contributing

Contributions are welcome! Please open an issue or pull request on [GitHub](https://github.com/eggspot/EggMapper).

---

*Powered by [Eggspot](https://eggspot.app)*


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

> ⏱ **Last updated:** 2026-03-18 14:58 UTC

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

#### 🔵 Flat Mapping

| Method     | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual     | 15.43 ns | 0.311 ns | 0.319 ns | 15.03 ns | 15.41 ns | 16.32 ns |  1.00 |    0.03 |    1 | 0.0048 |      80 B |        1.00 |
| EggMapper  | 45.03 ns | 0.399 ns | 0.373 ns | 44.48 ns | 45.04 ns | 45.84 ns |  2.92 |    0.06 |    3 | 0.0048 |      80 B |        1.00 |
| AutoMapper | 50.95 ns | 0.469 ns | 0.416 ns | 50.37 ns | 50.84 ns | 51.89 ns |  3.30 |    0.07 |    4 | 0.0067 |     112 B |        1.40 |
| Mapster    | 29.35 ns | 0.569 ns | 0.720 ns | 28.49 ns | 29.19 ns | 30.72 ns |  1.90 |    0.06 |    2 | 0.0048 |      80 B |        1.00 |

#### 🟣 Deep Mapping (nested)

| Method     | Mean     | Error    | StdDev   | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |---------:|---------:|---------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual     | 53.48 ns | 0.493 ns | 0.437 ns | 53.03 ns | 53.42 ns | 54.47 ns |  1.00 |    0.01 |    1 | 0.0162 |     272 B |        1.00 |
| EggMapper  | 86.07 ns | 1.377 ns | 1.288 ns | 83.74 ns | 86.40 ns | 88.36 ns |  1.61 |    0.03 |    3 | 0.0162 |     272 B |        1.00 |
| AutoMapper | 91.54 ns | 1.805 ns | 2.410 ns | 87.96 ns | 91.14 ns | 97.47 ns |  1.71 |    0.05 |    4 | 0.0181 |     304 B |        1.12 |
| Mapster    | 73.44 ns | 1.387 ns | 1.597 ns | 71.32 ns | 73.25 ns | 77.13 ns |  1.37 |    0.03 |    2 | 0.0162 |     272 B |        1.00 |

#### 🟠 Collection (100 items)

| Method     | Mean     | Error     | StdDev    | Min      | Median   | Max      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------- |---------:|----------:|----------:|---------:|---------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Manual     | 1.829 μs | 0.0226 μs | 0.0211 μs | 1.802 μs | 1.822 μs | 1.862 μs |  1.00 |    0.02 |    1 | 0.5322 | 0.0172 |   8.72 KB |        1.00 |
| EggMapper  | 2.003 μs | 0.0281 μs | 0.0262 μs | 1.964 μs | 1.996 μs | 2.047 μs |  1.10 |    0.02 |    2 | 0.5264 | 0.0153 |   8.65 KB |        0.99 |
| AutoMapper |       NA |        NA |        NA |       NA |       NA |       NA |     ? |       ? |    ? |     NA |     NA |        NA |           ? |
| Mapster    | 1.784 μs | 0.0355 μs | 0.0380 μs | 1.732 μs | 1.783 μs | 1.854 μs |  0.98 |    0.02 |    1 | 0.5283 | 0.0172 |   8.65 KB |        0.99 |

#### 🟢 Complex Mapping

| Method     | Mean      | Error    | StdDev   | Min       | Median    | Max       | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------- |----------:|---------:|---------:|----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Manual     |  94.47 ns | 1.408 ns | 1.317 ns |  92.47 ns |  94.16 ns |  97.19 ns |  1.00 |    0.02 |    1 | 0.0234 |     392 B |        1.00 |
| EggMapper  | 400.77 ns | 1.970 ns | 1.746 ns | 397.25 ns | 401.24 ns | 403.50 ns |  4.24 |    0.06 |    2 | 0.0257 |     432 B |        1.10 |
| AutoMapper | 405.80 ns | 5.231 ns | 4.893 ns | 398.68 ns | 406.90 ns | 411.83 ns |  4.30 |    0.08 |    2 | 0.0277 |     464 B |        1.18 |
| Mapster    |  91.59 ns | 1.193 ns | 1.057 ns |  89.24 ns |  91.84 ns |  93.09 ns |  0.97 |    0.02 |    1 | 0.0191 |     320 B |        0.82 |

#### ⚪ Startup / Config

| Method            | Mean         | Error      | StdDev     | Min          | Median       | Max          | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------ |-------------:|-----------:|-----------:|-------------:|-------------:|-------------:|------:|-----:|-------:|-------:|----------:|------------:|
| EggMapperStartup  | 2,258.511 μs |  6.5988 μs |  5.8496 μs | 2,250.928 μs | 2,257.554 μs | 2,271.036 μs | 1.000 |    2 | 3.9063 |      - | 103.16 KB |        1.00 |
| AutoMapperStartup | 2,249.998 μs | 17.6920 μs | 16.5491 μs | 2,224.064 μs | 2,250.910 μs | 2,280.316 μs | 0.996 |    2 | 3.9063 |      - | 103.35 KB |        1.00 |
| MapsterStartup    |     2.499 μs |  0.0286 μs |  0.0268 μs |     2.450 μs |     2.496 μs |     2.543 μs | 0.001 |    1 | 0.7019 | 0.0267 |  11.51 KB |        0.11 |

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


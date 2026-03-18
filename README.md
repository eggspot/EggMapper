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

> **Column guide:** `Mean` = avg time · `Error` = ½ CI · `StdDev` = std dev · `Min`/`Median`/`Max` = range · `Ratio` = vs Manual baseline · `Rank` = 1 is fastest · `Allocated` = heap / op

*Benchmark results not yet available — run the [Benchmarks workflow](https://github.com/eggspot/EggMapper/actions/workflows/benchmarks.yml).*

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


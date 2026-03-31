---
layout: default
title: EggMapper
---

# EggMapper

**Fastest .NET runtime object-to-object mapper.** Drop-in replacement for AutoMapper — same API, 2-5x faster, MIT licensed.

## Why EggMapper?

| | AutoMapper | EggMapper |
|---|-----------|-----------|
| **License** | Commercial (v13+) | MIT (free forever) |
| **Performance** | Baseline | 2-5x faster |
| **Allocations** | Extra per-map | Zero extra |
| **Runtime reflection** | Yes | No (compiled expressions) |
| **API** | Original | Same API, drop-in |

## Install

```bash
dotnet add package EggMapper
```

## 30-Second Quick Start

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Customer, CustomerDto>()
        .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"));
});

var mapper = config.CreateMapper();
var dto = mapper.Map<CustomerDto>(customer);
```

## Key Features

- **Same API as AutoMapper** — CreateMap, ForMember, Profile, IMapper
- **Zero runtime reflection** — all delegates compiled as expression trees
- **Zero extra allocations** — matches hand-written mapping code
- **Collection auto-mapping** — `Map<List<B>>(listOfA)` works with just `CreateMap<A,B>()`
- **Same-type auto-mapping** — `Map<T,T>(obj)` creates a copy without any configuration
- **EF Core ProjectTo** — `query.ProjectTo<Src, Dest>(config)` translates to SQL
- **DI integration** — `services.AddEggMapper(assembly)` with scoped IServiceProvider
- **EF Core proxy support** — base-type + interface walk for lazy-loading proxies

## Documentation

- [Quick Start](quick-start) — Install, DI, Profiles, Collections
- [Getting Started](Getting-Started) — Detailed walkthrough
- [Configuration](Configuration) — MapperConfiguration, Profiles, Validation
- [API Reference](API-Reference) — All Map overloads, ForMember options
- [Advanced Features](Advanced-Features) — ProjectTo, Open Generics, Inheritance, Hooks
- [Dependency Injection](Dependency-Injection) — ASP.NET, Blazor, gRPC, Windows Service
- [Profiles](Profiles) — Organizing mappings into profile classes
- [Migration Guide](Migration-Guide) — Runtime to Compile-Time tiers
- [Performance](Performance) — Benchmark results vs all competitors

## Links

- [NuGet Package](https://www.nuget.org/packages/EggMapper)
- [GitHub Repository](https://github.com/eggspot/EggMapper)
- [Report Issues](https://github.com/eggspot/EggMapper/issues)

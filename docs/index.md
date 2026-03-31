---
layout: default
title: Home
nav_order: 1
description: "EggMapper — fastest .NET object mapper. Free AutoMapper alternative, 2-5x faster, MIT licensed."
permalink: /
---

# EggMapper
{: .fs-9 }

Fastest .NET runtime object-to-object mapper. Drop-in AutoMapper replacement — same API, 2-5x faster, MIT licensed.
{: .fs-6 .fw-300 }

[Get Started](quick-start){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/eggspot/EggMapper){: .btn .fs-5 .mb-4 .mb-md-0 }

---

## Why EggMapper?

| | AutoMapper | **EggMapper** |
|---|-----------|-----------|
| License | Commercial (v13+) | **MIT (free forever)** |
| Performance | Baseline | **2-5x faster** |
| Allocations | Extra per-map | **Zero extra** |
| Runtime reflection | Yes | **No** (compiled expressions) |
| API | Original | **Same API, drop-in** |

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
- **Same-type auto-mapping** — `Map<T,T>(obj)` creates a copy without configuration
- **EF Core ProjectTo** — `query.ProjectTo<Src, Dest>(config)` translates to SQL
- **DI integration** — `services.AddEggMapper(assembly)` with scoped IServiceProvider
- **EF Core proxy support** — base-type + interface walk for lazy-loading proxies
- **Patch mapping** — partial updates with `Patch<S,D>(source, dest)`
- **Open generics** — `CreateMap(typeof(Result<>), typeof(ResultDto<>))`

## Links

- [NuGet Package](https://www.nuget.org/packages/EggMapper)
- [GitHub Repository](https://github.com/eggspot/EggMapper)
- [Report Issues](https://github.com/eggspot/EggMapper/issues)

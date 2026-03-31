---
layout: default
title: Home
nav_order: 1
description: "EggMapper — fastest .NET object mapper. Free AutoMapper alternative, 2-5x faster, MIT licensed."
permalink: /
---

# EggMapper
{: .fs-9 }

The fastest .NET runtime object mapper. Drop-in AutoMapper replacement — same API, 2-5x faster, zero allocations, MIT licensed.
{: .fs-6 .fw-300 }

AutoMapper went commercial. You need a free, faster alternative. EggMapper is that alternative — swap the NuGet package, change one `using`, and your app gets 2-5x faster mapping with zero code changes.
{: .fs-5 .fw-300 }

[Get Started in 30 Seconds](quick-start){: .btn .btn-primary .fs-5 .mb-4 .mb-md-0 .mr-2 }
[View on GitHub](https://github.com/eggspot/EggMapper){: .btn .fs-5 .mb-4 .mb-md-0 }
[Migration from AutoMapper](vs-automapper){: .btn .btn-outline .fs-5 .mb-4 .mb-md-0 }

---

## Why EggMapper?

| | AutoMapper | **EggMapper** |
|---|-----------|-----------|
| **License** | Commercial RPL (v13+) | **MIT (free forever)** |
| **Performance** | Baseline | **2-5x faster** |
| **Allocations** | Extra per-map | **Zero extra** |
| **Runtime reflection** | Yes | **No** (compiled expressions) |
| **API** | Original | **Same API, drop-in** |

## Install

```bash
dotnet add package EggMapper
```

Supports `netstandard2.0`, `net462`, `net8.0`, `net9.0`, `net10.0`.

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

## Real-World Example: EF Core Entity to API Response

```csharp
using EggMapper;

// Profile — group related maps
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName));

        CreateMap<OrderLine, OrderLineResponse>();
    }
}

// Minimal API endpoint
app.MapGet("/orders/{id}", async (int id, AppDbContext db, IMapper mapper) =>
{
    var order = await db.Orders
        .Include(o => o.Customer)
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == id);

    return order is null
        ? Results.NotFound()
        : Results.Ok(mapper.Map<Order, OrderResponse>(order));
});
```

## Key Features

| Category | Feature |
|----------|---------|
| **API** | Same as AutoMapper — `CreateMap`, `ForMember`, `Profile`, `IMapper` |
| **Performance** | Zero runtime reflection, zero extra allocations, compiled expression trees |
| **Collections** | Auto-mapping `Map<List<B>>(listOfA)` + batch `MapList<A,B>()` with inlined loop |
| **EF Core** | `ProjectTo<S,D>(config)` translates to SQL; proxy support for lazy-loading |
| **Cloning** | `Map<T,T>(obj)` creates a deep copy without configuration |
| **Patch** | `Patch<S,D>(source, dest)` for partial updates |
| **DI** | One-line setup: `services.AddEggMapper(assembly)` |
| **Advanced** | Open generics, inline validation, constructor and record mapping |
| **Code Gen** | Optional source generators (`[MapTo]`, `[EggMapper]`) for compile-time mapping |

## Links

- [NuGet Package](https://www.nuget.org/packages/EggMapper)
- [GitHub Repository](https://github.com/eggspot/EggMapper)
- [Report Issues](https://github.com/eggspot/EggMapper/issues)

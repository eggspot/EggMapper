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
// Entities
public class Order
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public Customer Customer { get; set; } = null!;
    public List<OrderLine> Lines { get; set; } = [];
}

public class OrderLine
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

// DTOs
public record OrderResponse(
    int Id,
    DateTime CreatedAt,
    decimal Total,
    string CustomerName,
    List<OrderLineResponse> Lines);

public record OrderLineResponse(int ProductId, string ProductName, int Quantity, decimal UnitPrice);

// Profile
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName));

        CreateMap<OrderLine, OrderLineResponse>();
    }
}

// Usage in a minimal API endpoint
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

- **Same API as AutoMapper** — CreateMap, ForMember, Profile, IMapper
- **Zero runtime reflection** — all delegates compiled as expression trees
- **Zero extra allocations** — matches hand-written mapping code
- **Collection auto-mapping** — `Map<List<B>>(listOfA)` works with just `CreateMap<A,B>()`
- **Batch collection mapping** — `MapList<A,B>(list)` uses a fully inlined compiled loop
- **Same-type auto-mapping** — `Map<T,T>(obj)` creates a deep copy without configuration
- **EF Core ProjectTo** — `query.ProjectTo<Src, Dest>(config)` translates to SQL
- **Patch mapping** — partial updates with `Patch<S,D>(source, dest)`
- **DI integration** — `services.AddEggMapper(assembly)` one-line setup
- **EF Core proxy support** — base-type + interface walk for lazy-loading proxies
- **Open generics** — `CreateMap(typeof(Result<>), typeof(ResultDto<>))`
- **Inline validation** — `.Validate(d => d.Email, e => e.Contains("@"), "Invalid")`
- **Constructor & record mapping** — auto-selects best-matching constructor

## Links

- [NuGet Package](https://www.nuget.org/packages/EggMapper)
- [GitHub Repository](https://github.com/eggspot/EggMapper)
- [Report Issues](https://github.com/eggspot/EggMapper/issues)

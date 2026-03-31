---
layout: default
title: Getting Started
nav_order: 11
description: "Getting started with EggMapper — install, define types, configure, map."
---

# Getting Started
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Installation

### .NET CLI

```bash
dotnet add package EggMapper
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package EggMapper
```

DI support (`AddEggMapper()`) is included in the main package. No separate package needed.
{: .note }

---

## Your First Mapping

### 1 -- Define your types

```csharp
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2 -- Create a configuration

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
});
```

The `MapperConfiguration` constructor **compiles** expression-tree delegates for every registered map. Keep a single long-lived instance (singleton).

### 3 -- Create a mapper and map

```csharp
IMapper mapper = config.CreateMapper();

var order = new Order
{
    Id = 1,
    CustomerName = "Alice",
    Total = 99.99m,
    CreatedAt = DateTime.UtcNow
};

OrderDto dto = mapper.Map<Order, OrderDto>(order);
// dto.Id == 1, dto.CustomerName == "Alice", dto.Total == 99.99
```

You can also use the non-generic overload when the source type is not known at compile time:

```csharp
// Source type inferred from the argument at runtime
OrderDto dto2 = mapper.Map<OrderDto>(order);
```

The generic `Map<TSrc, TDst>()` overload is faster than `Map<TDst>(object)` because it uses the static generic cache. Prefer the generic version in hot paths.
{: .note }

---

## Mapping with Custom Members

When property names differ between source and destination, use `ForMember`:

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Customer, CustomerDto>()
        .ForMember(d => d.FullName,
                   o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
        .ForMember(d => d.City,
                   o => o.MapFrom(s => s.Address.City))
        .ForMember(d => d.InternalId, o => o.Ignore());
});
```

---

## Mapping an Existing Destination

Pass a pre-existing destination object as the second argument to populate it instead of allocating a new one:

```csharp
var existing = new OrderDto { Id = 99 };
mapper.Map(order, existing);   // existing is populated in-place
// existing.Id is now order.Id, existing.CustomerName is order.CustomerName, etc.
```

This is useful for updating tracked EF Core entities from a request DTO:

```csharp
var entity = await db.Orders.FindAsync(id);
mapper.Map(updateDto, entity);
await db.SaveChangesAsync();
```

---

## Mapping Collections

EggMapper maps common collection types automatically when the element map is registered:

### Using `MapList` (recommended for performance)

```csharp
List<Order> orders = await db.Orders.ToListAsync();

// Fully inlined compiled loop — near-manual speed
List<OrderDto> dtos = mapper.MapList<Order, OrderDto>(orders);
```

### Using `Map` with collection types

```csharp
cfg.CreateMap<Order, OrderDto>();

// Auto-detects collection types
var dtos = mapper.Map<List<OrderDto>>(orders);
```

### Nested collections

```csharp
// Customer has List<Order>, Order has List<OrderLine>
cfg.CreateMap<Customer, CustomerDto>();
cfg.CreateMap<Order, OrderDto>();
cfg.CreateMap<OrderLine, OrderLineDto>();

// Nested collections are mapped automatically
var dto = mapper.Map<Customer, CustomerDto>(customer);
// dto.Orders[0].Lines[0].ProductName is populated
```

---

## Mapping Records and Immutable Types

EggMapper automatically selects the best-matching constructor:

```csharp
public record OrderDto(int Id, string CustomerName, decimal Total);

cfg.CreateMap<Order, OrderDto>();
// Calls: new OrderDto(src.Id, src.CustomerName, src.Total)
```

Records with additional init properties:

```csharp
public record ProductDto(int Id, string Name)
{
    public decimal Price { get; init; }
    public bool InStock { get; init; }
}

cfg.CreateMap<Product, ProductDto>();
// new ProductDto(src.Id, src.Name) { Price = src.Price, InStock = src.StockQuantity > 0 }
```

---

## Using with DI (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);

// Any service or controller
public class OrderService(IMapper mapper, AppDbContext db)
{
    public async Task<OrderDto> GetOrderAsync(int id)
    {
        var order = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == id);

        return mapper.Map<Order, OrderDto>(order!);
    }

    public async Task<List<OrderSummaryDto>> GetRecentOrdersAsync()
    {
        var orders = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Take(50)
            .ToListAsync();

        return mapper.MapList<Order, OrderSummaryDto>(orders);
    }
}
```

---

## Next Steps

| Topic | Link |
|-------|------|
| Custom member mappings, ignores, reverse maps | [Advanced Features](Advanced-Features) |
| Organise maps into reusable classes | [Profiles](Profiles) |
| Use EggMapper in ASP.NET Core, Blazor, gRPC | [Dependency Injection](Dependency-Injection) |
| Full configuration options | [Configuration](Configuration) |
| Compare with AutoMapper | [EggMapper vs AutoMapper](vs-automapper) |
| Benchmark methodology and results | [Performance](Performance) |
| Complete API surface | [API Reference](API-Reference) |

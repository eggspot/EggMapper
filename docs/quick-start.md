---
layout: default
title: Quick Start
---

# Quick Start

## Installation

```bash
dotnet add package EggMapper
```

No separate DI package needed — `AddEggMapper()` is included.

Supported frameworks: `netstandard2.0`, `net462`, `net8.0`, `net9.0`, `net10.0`

## Basic Usage

```csharp
using EggMapper;

// 1. Configure mappings
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Product, ProductDto>();
});

// 2. Create mapper
var mapper = config.CreateMapper();

// 3. Map
var dto = mapper.Map<ProductDto>(product);
```

## Dependency Injection (ASP.NET / Blazor / gRPC)

```csharp
// Program.cs
builder.Services.AddEggMapper(typeof(MyProfile).Assembly);

// Controller or service — inject IMapper
public class ProductsController(IMapper mapper)
{
    public IActionResult Get(int id)
    {
        var product = db.Products.Find(id);
        return Ok(mapper.Map<ProductDto>(product));
    }
}
```

`IMapper` is registered as **Transient** (each injection gets a fresh instance with the caller's scoped `IServiceProvider`). `MapperConfiguration` is **Singleton**.

## Profiles

Organize mappings into profile classes:

```csharp
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.Secret, o => o.Ignore());

        CreateMap<OrderItem, OrderItemDto>();
    }
}

// Register all profiles from an assembly
builder.Services.AddEggMapper(typeof(ProductProfile).Assembly);
```

## Collection Mapping

```csharp
// Optimized batch mapping
List<ProductDto> dtos = mapper.MapList<Product, ProductDto>(products);

// Also works via Map<> — auto-detects collections
var dtos = mapper.Map<List<ProductDto>>(products);
```

No `CreateMap<List<Product>, List<ProductDto>>()` needed — just the element map.

## EF Core ProjectTo

```csharp
// Translates to SQL — no in-memory mapping
var dtos = await dbContext.Products
    .Where(p => p.IsActive)
    .ProjectTo<Product, ProductDto>(mapperConfig)
    .ToListAsync();
```

## Same-Type Mapping (Cloning)

```csharp
// No CreateMap needed — auto-compiles on first use
var copy = mapper.Map<Customer, Customer>(customer);
```

## Configuration Validation

```csharp
var config = new MapperConfiguration(cfg => { ... });
config.AssertConfigurationIsValid(); // Throws if any dest members unmapped
```

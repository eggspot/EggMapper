---
layout: default
title: Quick Start
nav_order: 2
description: "Get started with EggMapper — installation, DI setup, profiles, collections, ProjectTo."
---

# Quick Start
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Installation

```bash
dotnet add package EggMapper
```

No separate DI package needed — `AddEggMapper()` is included.

Supported: `netstandard2.0`, `net462`, `net8.0`, `net9.0`, `net10.0`

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

## Dependency Injection

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

`IMapper` is **Transient** (fresh per injection with caller's scoped `IServiceProvider`). `MapperConfiguration` is **Singleton**.

## Profiles

```csharp
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.First} {s.Last}"))
            .ForMember(d => d.Secret, o => o.Ignore());

        CreateMap<OrderItem, OrderItemDto>();
    }
}
```

## Collection Mapping

```csharp
// Optimized batch mapping
List<ProductDto> dtos = mapper.MapList<Product, ProductDto>(products);

// Also works via Map<> — auto-detects collections
var dtos = mapper.Map<List<ProductDto>>(products);
```

No `CreateMap<List<Product>, List<ProductDto>>()` needed.

## EF Core ProjectTo

```csharp
var dtos = await dbContext.Products
    .Where(p => p.IsActive)
    .ProjectTo<Product, ProductDto>(mapperConfig)
    .ToListAsync();
```

Translates to SQL — no in-memory mapping.

## Same-Type Mapping (Cloning)

```csharp
// No CreateMap needed — auto-compiles on first use
var copy = mapper.Map<Customer, Customer>(customer);
```

## Validation

```csharp
config.AssertConfigurationIsValid();
// Throws if any destination members are unmapped
```

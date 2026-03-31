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

The `MapperConfiguration` constructor compiles all registered type maps into cached delegates. Create it once and keep it as a singleton.
{: .note }

## Dependency Injection

### ASP.NET Core Web API

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);

var app = builder.Build();

app.MapGet("/products/{id}", async (int id, AppDbContext db, IMapper mapper) =>
{
    var product = await db.Products.FindAsync(id);
    return product is null
        ? Results.NotFound()
        : Results.Ok(mapper.Map<Product, ProductDto>(product));
});

app.Run();
```

### ASP.NET Core MVC Controller

```csharp
public class ProductsController(IMapper mapper, AppDbContext db) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        return Ok(mapper.Map<Product, ProductDto>(product));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await db.Products.ToListAsync();
        return Ok(mapper.MapList<Product, ProductDto>(products));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        var product = await db.Products.FindAsync(id);
        if (product is null) return NotFound();

        mapper.Patch(request, product);  // Only non-null fields overwrite
        await db.SaveChangesAsync();
        return NoContent();
    }
}
```

### Blazor Server / Blazor InteractiveServer

```csharp
// Program.cs
builder.Services.AddEggMapper(typeof(ProductProfile).Assembly);

// Component
@inject IMapper Mapper

@code {
    private List<ProductViewModel> _products = [];

    protected override async Task OnInitializedAsync()
    {
        var entities = await DbContext.Products.ToListAsync();
        _products = Mapper.MapList<Product, ProductViewModel>(entities);
    }
}
```

### Blazor WebAssembly

```csharp
// Program.cs (client-side)
builder.Services.AddEggMapper(typeof(ProductProfile).Assembly);

// In a service
public class ProductService(HttpClient http, IMapper mapper)
{
    public async Task<List<ProductViewModel>> GetProductsAsync()
    {
        var apiModels = await http.GetFromJsonAsync<List<ProductApiModel>>("api/products")
            ?? [];
        return mapper.MapList<ProductApiModel, ProductViewModel>(apiModels);
    }
}
```

### gRPC Service

```csharp
// Program.cs
builder.Services.AddGrpc();
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);

// gRPC service implementation
public class OrderGrpcService(IMapper mapper, OrderRepository repo)
    : OrderService.OrderServiceBase
{
    public override async Task<OrderReply> GetOrder(
        OrderRequest request, ServerCallContext context)
    {
        var order = await repo.GetByIdAsync(request.Id);
        return mapper.Map<Order, OrderReply>(order);
    }
}
```

### Worker Service / Background Service

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddEggMapper(typeof(SyncProfile).Assembly);
builder.Services.AddHostedService<DataSyncWorker>();

// Worker
public class DataSyncWorker(IMapper mapper, ILogger<DataSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var externalRecords = await FetchFromExternalApi();
            var entities = mapper.MapList<ExternalRecord, LocalEntity>(externalRecords);
            await SaveToDatabase(entities);

            logger.LogInformation("Synced {Count} records", entities.Count);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

`IMapper` is **Singleton** (backed by the compiled delegate cache). `MapperConfiguration` is also **Singleton**.
{: .note }

## Profiles

Group related maps into profile classes for organisation:

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName))
            .ForMember(d => d.Total, o => o.MapFrom(s => s.Lines.Sum(l => l.Quantity * l.UnitPrice)));

        CreateMap<OrderLine, OrderLineDto>();

        CreateMap<Order, OrderSummaryDto>()
            .ForMember(d => d.LineCount, o => o.MapFrom(s => s.Lines.Count));
    }
}

public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.PasswordHash, o => o.Ignore());

        CreateMap<Address, AddressDto>();
    }
}
```

## Collection Mapping

```csharp
// Optimized batch mapping — fully inlined compiled loop
List<ProductDto> dtos = mapper.MapList<Product, ProductDto>(products);

// Also works via Map<> — auto-detects collections
var dtos2 = mapper.Map<List<ProductDto>>(products);
```

No `CreateMap<List<Product>, List<ProductDto>>()` needed. Register the element map and collection mapping works automatically.

`MapList<S,D>()` compiles the entire collection loop as a single expression tree. For large collections (100+ items), it is measurably faster than mapping elements individually.
{: .note }

## EF Core ProjectTo

```csharp
// Translates to SQL — no in-memory mapping, no entity materialization
var dtos = await dbContext.Products
    .Where(p => p.IsActive)
    .ProjectTo<Product, ProductDto>(mapperConfig)
    .ToListAsync();

// Works with complex queries
var orderSummaries = await dbContext.Orders
    .Where(o => o.CreatedAt >= cutoffDate)
    .OrderByDescending(o => o.Total)
    .ProjectTo<Order, OrderSummaryDto>(mapperConfig)
    .Take(50)
    .ToListAsync();
```

`ProjectTo` builds a pure `Expression<Func<TSource, TDest>>` and passes it to LINQ's `Select()`. EggMapper never calls `.Compile()` on this expression — the LINQ provider (EF Core) translates it to SQL.
{: .note }

## Same-Type Mapping (Cloning)

```csharp
// No CreateMap needed — auto-compiles on first use
var copy = mapper.Map<Customer, Customer>(customer);

// Useful for creating snapshots before mutation
var snapshot = mapper.Map<Order, Order>(order);
order.Total = 0;  // snapshot.Total still has the original value
```

## Patch Mapping (Partial Updates)

```csharp
// Only non-null / set properties overwrite the destination
var request = new UpdateProductRequest { Name = "New Name" };  // Price is null
var product = await db.Products.FindAsync(id);

mapper.Patch(request, product);
// product.Name = "New Name", product.Price unchanged

await db.SaveChangesAsync();
```

## Validation

```csharp
config.AssertConfigurationIsValid();
// Throws if any destination members are unmapped and not ignored
```

Call this in your test suite to catch configuration errors at build time rather than runtime.
{: .warning }

## Common Pitfalls

Do not create `MapperConfiguration` per-request. It compiles expression trees during construction, which is expensive. Always keep it as a singleton.
{: .warning }

- **Forgetting nested type maps** — If `CustomerDto` has an `AddressDto` property, you need both `CreateMap<Customer, CustomerDto>()` and `CreateMap<Address, AddressDto>()`.
- **Using the non-generic `Map<TDest>(object)` overload in hot paths** — The generic `Map<TSrc, TDst>(src)` is faster because it uses the static generic cache with zero dictionary lookups.
- **Not calling `AssertConfigurationIsValid()` in tests** — Missing maps silently leave destination properties at their default values.

---
layout: default
title: Dependency Injection
parent: Guide
nav_order: 3
description: "EggMapper DI integration — ASP.NET Core, Blazor, gRPC, Worker Service, minimal API setup."
---

# Dependency Injection
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

DI support is built into the main `EggMapper` package — no separate package needed.

## Installation

```bash
dotnet add package EggMapper
```

---

## ASP.NET Core Web API

### Minimal API

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register EggMapper — scans for all Profile subclasses
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);

var app = builder.Build();

// IMapper is injected directly into endpoint handlers
app.MapGet("/orders/{id}", async (int id, AppDbContext db, IMapper mapper) =>
{
    var order = await db.Orders
        .Include(o => o.Customer)
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == id);

    return order is null
        ? Results.NotFound()
        : Results.Ok(mapper.Map<Order, OrderDto>(order));
});

app.MapGet("/orders", async (AppDbContext db, IMapper mapper) =>
{
    var orders = await db.Orders.ToListAsync();
    return Results.Ok(mapper.MapList<Order, OrderSummaryDto>(orders));
});

// ProjectTo for read-only queries (translated to SQL)
app.MapGet("/products", async (AppDbContext db, MapperConfiguration config) =>
{
    var products = await db.Products
        .Where(p => p.IsActive)
        .ProjectTo<Product, ProductDto>(config)
        .ToListAsync();
    return Results.Ok(products);
});

app.MapPatch("/products/{id}", async (int id, UpdateProductRequest req, AppDbContext db, IMapper mapper) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    mapper.Patch(req, product);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();
```

### MVC Controller

```csharp
// Program.cs
builder.Services.AddControllers();
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);

// Controller — inject IMapper via primary constructor
public class OrdersController(IMapper mapper, AppDbContext db) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await db.Orders
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();
        return Ok(mapper.Map<Order, OrderDto>(order));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        var orders = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return Ok(mapper.MapList<Order, OrderSummaryDto>(orders));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var order = mapper.Map<CreateOrderRequest, Order>(request);
        db.Orders.Add(order);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = order.Id },
            mapper.Map<Order, OrderDto>(order));
    }
}
```

---

## Blazor Server / InteractiveServer

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddEggMapper(typeof(ProductProfile).Assembly);
```

```razor
@* Components/Pages/Products.razor *@
@page "/products"
@inject IMapper Mapper
@inject AppDbContext DbContext

<h3>Products</h3>

@foreach (var product in _products)
{
    <div>@product.Name — @product.Price.ToString("C")</div>
}

@code {
    private List<ProductViewModel> _products = [];

    protected override async Task OnInitializedAsync()
    {
        var entities = await DbContext.Products
            .Where(p => p.IsActive)
            .ToListAsync();

        _products = Mapper.MapList<Product, ProductViewModel>(entities);
    }
}
```

---

## Blazor WebAssembly (WASM)

```csharp
// Program.cs (client-side)
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddEggMapper(typeof(ProductProfile).Assembly);

await builder.Build().RunAsync();
```

```csharp
// Services/ProductService.cs
public class ProductService(HttpClient http, IMapper mapper)
{
    public async Task<List<ProductViewModel>> GetProductsAsync()
    {
        var apiModels = await http.GetFromJsonAsync<List<ProductApiResponse>>("api/products")
            ?? [];
        return mapper.MapList<ProductApiResponse, ProductViewModel>(apiModels);
    }

    public async Task<ProductDetailViewModel?> GetProductAsync(int id)
    {
        var apiModel = await http.GetFromJsonAsync<ProductDetailApiResponse>($"api/products/{id}");
        return apiModel is null ? null : mapper.Map<ProductDetailApiResponse, ProductDetailViewModel>(apiModel);
    }
}
```

---

## gRPC Service

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddGrpc();
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);

var app = builder.Build();
app.MapGrpcService<OrderGrpcService>();
app.Run();
```

```csharp
// Services/OrderGrpcService.cs
public class OrderGrpcService(IMapper mapper, AppDbContext db)
    : OrderService.OrderServiceBase
{
    public override async Task<GetOrderReply> GetOrder(
        GetOrderRequest request, ServerCallContext context)
    {
        var order = await db.Orders
            .Include(o => o.Lines)
            .FirstOrDefaultAsync(o => o.Id == request.Id);

        if (order is null)
            throw new RpcException(new Status(StatusCode.NotFound, "Order not found"));

        return mapper.Map<Order, GetOrderReply>(order);
    }

    public override async Task<ListOrdersReply> ListOrders(
        ListOrdersRequest request, ServerCallContext context)
    {
        var orders = await db.Orders
            .OrderByDescending(o => o.CreatedAt)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var reply = new ListOrdersReply();
        reply.Orders.AddRange(mapper.MapList<Order, OrderBrief>(orders));
        return reply;
    }
}
```

---

## Worker Service / Background Service

```csharp
// Program.cs
var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddEggMapper(typeof(SyncProfile).Assembly);
builder.Services.AddHostedService<DataSyncWorker>();

var host = builder.Build();
host.Run();
```

```csharp
// Workers/DataSyncWorker.cs
public class DataSyncWorker(
    IMapper mapper,
    IServiceScopeFactory scopeFactory,
    ILogger<DataSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var externalOrders = await FetchFromExternalApiAsync(stoppingToken);
            var entities = mapper.MapList<ExternalOrder, Order>(externalOrders);

            db.Orders.AddRange(entities);
            await db.SaveChangesAsync(stoppingToken);

            logger.LogInformation("Synced {Count} orders", entities.Count);
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
```

---

## Setup Options

### Scan an assembly for Profiles (recommended)

```csharp
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);
```

Pass one or more assemblies and every `Profile` subclass found will be registered automatically.

### Scan multiple assemblies

```csharp
builder.Services.AddEggMapper(
    typeof(OrderProfile).Assembly,    // Web layer profiles
    typeof(ReportProfile).Assembly);  // Reporting layer profiles
```

### Inline configuration (no profiles needed)

```csharp
builder.Services.AddEggMapper(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
    cfg.CreateMap<Customer, CustomerDto>();
    cfg.CreateMap<Address, AddressDto>();
});
```

For small projects with just a few maps, inline configuration is simpler. For larger projects, profiles keep things organized.
{: .note }

---

## What Gets Registered

| Service | Lifetime | Description |
|---------|----------|-------------|
| `MapperConfiguration` | Singleton | The compiled configuration (keeps the delegate cache) |
| `IMapper` | Singleton | Resolved from `MapperConfiguration.CreateMapper()` |

Both services are safe to inject anywhere. `MapperConfiguration` is immutable after construction, and `IMapper` is backed by the same immutable compiled cache.

---

## Injecting `IMapper`

### With primary constructors (C# 12)

```csharp
public class OrderService(IMapper mapper, AppDbContext db)
{
    public OrderDto GetOrder(int id)
    {
        var order = db.Orders.Find(id);
        return mapper.Map<Order, OrderDto>(order!);
    }
}
```

### Traditional constructor injection

```csharp
public class OrderService
{
    private readonly IMapper _mapper;
    private readonly AppDbContext _db;

    public OrderService(IMapper mapper, AppDbContext db)
    {
        _mapper = mapper;
        _db = db;
    }

    public OrderDto GetOrder(int id)
    {
        var order = _db.Orders.Find(id);
        return _mapper.Map<Order, OrderDto>(order!);
    }
}
```

### Injecting `MapperConfiguration` directly

You can also inject `MapperConfiguration` for `ProjectTo` or `BuildProjection`:

```csharp
public class ProductQueryService(MapperConfiguration config, AppDbContext db)
{
    public async Task<List<ProductDto>> GetActiveProductsAsync()
    {
        return await db.Products
            .Where(p => p.IsActive)
            .ProjectTo<Product, ProductDto>(config)
            .ToListAsync();
    }
}
```

---

## Testing with DI

Use `ServiceCollection` directly in unit tests without a full host:

```csharp
[Fact]
public void Should_MapOrderToDto()
{
    var services = new ServiceCollection();
    services.AddEggMapper(cfg =>
    {
        cfg.CreateMap<Order, OrderDto>();
        cfg.CreateMap<OrderLine, OrderLineDto>();
    });

    var provider = services.BuildServiceProvider();
    var mapper = provider.GetRequiredService<IMapper>();

    var order = new Order
    {
        Id = 1,
        CustomerName = "Alice",
        Total = 99.99m,
        Lines = [new OrderLine { ProductName = "Widget", Quantity = 2 }]
    };

    var dto = mapper.Map<Order, OrderDto>(order);

    dto.Id.Should().Be(1);
    dto.CustomerName.Should().Be("Alice");
    dto.Lines.Should().HaveCount(1);
}
```

### Validate all mappings in a single test

```csharp
[Fact]
public void AllMappings_ShouldBeValid()
{
    var services = new ServiceCollection();
    services.AddEggMapper(typeof(OrderProfile).Assembly);

    var provider = services.BuildServiceProvider();
    var config = provider.GetRequiredService<MapperConfiguration>();

    config.AssertConfigurationIsValid();
}
```

---

## Common Pitfalls

Do not register `MapperConfiguration` as scoped or transient. It compiles expression trees in its constructor. `AddEggMapper()` handles this correctly (singleton), but be aware if you register manually.
{: .warning }

- **Manually registering as transient** — If you bypass `AddEggMapper()` and register `MapperConfiguration` as transient, you will recompile all maps on every request. Always use singleton.
- **Forgetting to scan the right assembly** — If your profiles are in a separate class library, pass that assembly: `AddEggMapper(typeof(SomeProfile).Assembly)`.
- **Using `IMapper` in a static context** — `IMapper` is designed for DI. In static helpers or extension methods, inject `MapperConfiguration` and call `CreateMapper()`.

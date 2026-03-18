# Dependency Injection

EggMapper ships a dedicated integration package for `Microsoft.Extensions.DependencyInjection` (used by ASP.NET Core and the generic host).

## Installation

```bash
dotnet add package EggMapper.DependencyInjection
```

---

## Setup in `Program.cs`

### Scan an assembly for Profiles (recommended)

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;

builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);
```

Pass one or more assemblies and every `Profile` subclass found will be registered automatically.

### Scan multiple assemblies

```csharp
builder.Services.AddEggMapper(
    typeof(OrderProfile).Assembly,
    typeof(ReportProfile).Assembly);
```

### Inline configuration (no profiles needed)

```csharp
builder.Services.AddEggMapper(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
    cfg.CreateMap<Customer, CustomerDto>();
});
```

---

## Injecting `IMapper`

`IMapper` is registered as a **singleton**. Inject it wherever you need it:

```csharp
public class OrderService
{
    private readonly IMapper _mapper;

    public OrderService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public OrderDto GetOrder(int id)
    {
        var order = _repository.Find(id);
        return _mapper.Map<Order, OrderDto>(order);
    }
}
```

### With primary constructors (.NET 8+)

```csharp
public class OrderService(IMapper mapper)
{
    public OrderDto GetOrder(int id)
        => mapper.Map<Order, OrderDto>(_repository.Find(id));
}
```

---

## Minimal API example

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEggMapper(typeof(Program).Assembly);

var app = builder.Build();

app.MapGet("/orders/{id}", (int id, IMapper mapper, OrderRepository repo) =>
{
    var order = repo.Find(id);
    return order is null ? Results.NotFound() : Results.Ok(mapper.Map<OrderDto>(order));
});

app.Run();
```

---

## What Gets Registered

| Service | Lifetime | Description |
|---------|----------|-------------|
| `MapperConfiguration` | Singleton | The compiled configuration (keeps the delegate cache) |
| `IMapper` | Singleton | Resolved from `MapperConfiguration.CreateMapper()` |

---

## Testing with DI

Use `ServiceCollection` directly in unit tests without a full host:

```csharp
var services = new ServiceCollection();
services.AddEggMapper(cfg => cfg.CreateMap<Order, OrderDto>());

var provider = services.BuildServiceProvider();
var mapper   = provider.GetRequiredService<IMapper>();

var dto = mapper.Map<OrderDto>(new Order { Id = 1 });
Assert.Equal(1, dto.Id);
```

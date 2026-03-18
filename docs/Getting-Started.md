# Getting Started

## Installation

### NuGet Package Manager

```bash
dotnet add package EggMapper
```

### Package Manager Console (Visual Studio)

```powershell
Install-Package EggMapper
```

### For ASP.NET Core / DI projects

```bash
dotnet add package EggMapper.DependencyInjection
```

---

## Your First Mapping

### 1 — Define your types

```csharp
public class Order
{
    public int    Id          { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Total      { get; set; }
}

public class OrderDto
{
    public int    Id          { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Total      { get; set; }
}
```

### 2 — Create a configuration

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
});
```

The `MapperConfiguration` constructor **compiles** expression-tree delegates for every registered map. Keep a single long-lived instance (singleton).

### 3 — Create a mapper and map

```csharp
IMapper mapper = config.CreateMapper();

var order = new Order { Id = 1, CustomerName = "Alice", Total = 99.99m };

OrderDto dto = mapper.Map<Order, OrderDto>(order);
// dto.Id == 1, dto.CustomerName == "Alice", dto.Total == 99.99
```

You can also use the non-generic overloads:

```csharp
// Source type inferred from the argument
OrderDto dto2 = mapper.Map<OrderDto>(order);
```

---

## Mapping an Existing Destination

Pass a pre-existing destination object as the second argument to populate it instead of allocating a new one:

```csharp
var existing = new OrderDto { Id = 99 };
mapper.Map(order, existing);   // existing is populated in-place
```

---

## Mapping Collections

EggMapper maps common collection types automatically:

```csharp
List<Order> orders = GetOrders();

// Map to a new list
List<OrderDto> dtos = orders
    .Select(o => mapper.Map<Order, OrderDto>(o))
    .ToList();
```

Or declare a `List<T>` → `List<T>` mapping for convenience:

```csharp
cfg.CreateMap<Order, OrderDto>();
// The mapper can handle IEnumerable<Order> → List<OrderDto> automatically
// when the collection element types are registered.
```

---

## Next Steps

| Topic | Link |
|-------|------|
| Custom member mappings, ignores, reverse maps | [Advanced Features](Advanced-Features) |
| Organise maps into reusable classes | [Profiles](Profiles) |
| Use EggMapper in ASP.NET Core | [Dependency Injection](Dependency-Injection) |
| Full configuration options | [Configuration](Configuration) |

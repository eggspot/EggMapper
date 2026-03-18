# Profiles

Profiles let you group related `CreateMap` calls into a reusable class. This keeps your mapping configuration organised as the application grows.

---

## Creating a Profile

Inherit from `EggMapper.Profile` and call `CreateMap` inside the constructor:

```csharp
using EggMapper;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName,
                       opt => opt.MapFrom(s => s.Customer.FullName));

        CreateMap<OrderLine, OrderLineDto>();
    }
}
```

---

## Registering Profiles

### Single profile

```csharp
var config = new MapperConfiguration(cfg =>
    cfg.AddProfile<OrderProfile>());
```

### Multiple profiles

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<OrderProfile>();
    cfg.AddProfile<CustomerProfile>();
    cfg.AddProfile<ProductProfile>();
});
```

### Scan an assembly (recommended for large projects)

```csharp
var config = new MapperConfiguration(cfg =>
    cfg.AddProfiles(typeof(OrderProfile).Assembly));
```

All classes that inherit `Profile` in the provided assembly are discovered and registered automatically.

---

## Dependency Injection with Profiles

When using the DI integration package, pass the assembly directly:

```csharp
// Program.cs
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);
```

See [Dependency Injection](Dependency-Injection) for details.

---

## Profile-Level Configuration

You may call any `IMappingExpression` fluent methods inside a profile just as you would inline:

```csharp
public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName,
                       opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.ShippingCity,
                       opt => opt.MapFrom(s => s.ShippingAddress.City))
            .ForMember(d => d.InternalCode,
                       opt => opt.Ignore())
            .ReverseMap();
    }
}
```

---

## Best Practices

- **One profile per aggregate root** — e.g. `OrderProfile` maps `Order`, `OrderLine`, `OrderStatus`.
- **Keep profiles in the same assembly as the types they map** — makes discovery straightforward.
- **Call `config.AssertConfigurationIsValid()`** in your test suite to catch unmapped properties early.

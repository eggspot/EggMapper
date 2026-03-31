---
layout: default
title: Profiles
parent: Guide
nav_order: 2
description: "EggMapper profiles — group related mappings into reusable Profile classes."
---

# Profiles
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

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

When using the DI integration, pass the assembly directly:

```csharp
// Program.cs
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);
```

See [Dependency Injection](Dependency-Injection) for details.

---

## Real-World Profile Examples

### E-commerce order aggregate

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // Entity -> DTO (read operations)
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName))
            .ForMember(d => d.ShippingCity, o => o.MapFrom(s => s.ShippingAddress.City))
            .ForMember(d => d.TotalWithTax, o => o.MapFrom(s => s.Total * (1 + s.TaxRate)));

        CreateMap<Order, OrderSummaryDto>()
            .ForMember(d => d.LineCount, o => o.MapFrom(s => s.Lines.Count))
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName));

        CreateMap<OrderLine, OrderLineDto>();
        CreateMap<OrderLine, OrderLineSummaryDto>();

        // Request -> Entity (write operations)
        CreateMap<CreateOrderRequest, Order>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore())
            .ForMember(d => d.Status, o => o.Ignore());

        CreateMap<CreateOrderLineRequest, OrderLine>()
            .ForMember(d => d.Id, o => o.Ignore());

        // Patch request
        CreateMap<UpdateOrderRequest, Order>();
    }
}
```

### Customer aggregate with address

```csharp
public class CustomerProfile : Profile
{
    public CustomerProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.PasswordHash, o => o.Ignore())
            .ForMember(d => d.SecurityStamp, o => o.Ignore());

        CreateMap<Customer, CustomerSummaryDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.OrderCount, o => o.MapFrom(s => s.Orders.Count));

        CreateMap<Address, AddressDto>();

        CreateMap<CreateCustomerRequest, Customer>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore());

        // Bidirectional
        CreateMap<Address, AddressDto>().ReverseMap();
    }
}
```

### Product catalog with categories

```csharp
public class ProductProfile : Profile
{
    public ProductProfile()
    {
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.CategoryName, o => o.MapFrom(s => s.Category.Name))
            .ForMember(d => d.InStock, o => o.MapFrom(s => s.StockQuantity > 0));

        CreateMap<Product, ProductDetailDto>()
            .ForMember(d => d.CategoryPath, o => o.MapFrom(s => BuildCategoryPath(s.Category)));

        CreateMap<Category, CategoryDto>()
            .MaxDepth(3); // Category tree

        CreateMap<ProductImage, ProductImageDto>();
    }

    private static string BuildCategoryPath(Category? cat)
    {
        var parts = new List<string>();
        while (cat is not null)
        {
            parts.Insert(0, cat.Name);
            cat = cat.Parent;
        }
        return string.Join(" > ", parts);
    }
}
```

---

## Profile Organisation Strategies

### By aggregate root (recommended)

One profile per domain aggregate:

```
Profiles/
  OrderProfile.cs          // Order, OrderLine, OrderStatus
  CustomerProfile.cs       // Customer, Address, ContactInfo
  ProductProfile.cs        // Product, Category, ProductImage
  PaymentProfile.cs        // Payment, CreditCardPayment, Refund
```

### By layer

Separate read and write mappings:

```
Profiles/
  ReadProfiles/
    OrderReadProfile.cs    // Order -> OrderDto, OrderSummaryDto
    CustomerReadProfile.cs // Customer -> CustomerDto
  WriteProfiles/
    OrderWriteProfile.cs   // CreateOrderRequest -> Order
    CustomerWriteProfile.cs
```

### By feature / module

For modular architectures:

```
Modules/
  Orders/
    OrderProfile.cs
  Inventory/
    ProductProfile.cs
  Reporting/
    ReportProfile.cs
```

---

## Best Practices

- **One profile per aggregate root** — e.g. `OrderProfile` maps `Order`, `OrderLine`, `OrderStatus`.
- **Keep profiles in the same assembly as the types they map** — makes discovery straightforward.
- **Avoid logic in profiles** — Profiles define _what_ maps to _what_. Complex logic belongs in services.
- **Use `Ignore()` explicitly** — Do not rely on properties being left at defaults. Explicit `Ignore()` makes intent clear and passes `AssertConfigurationIsValid()`.
- **Call `config.AssertConfigurationIsValid()`** in your test suite to catch unmapped properties early.

---

## Common Pitfalls

- **Duplicate type pair registrations** — If two profiles both call `CreateMap<Order, OrderDto>()`, the second one wins silently. Keep each type pair in exactly one profile.
- **Missing nested type maps** — If `OrderDto.Customer` is a `CustomerDto`, you need `CreateMap<Customer, CustomerDto>()` in the same or another profile. Use `AssertConfigurationIsValid()` to catch these.
- **Profile constructor with parameters** — Profile constructors must be parameterless. Do not inject services into profiles.

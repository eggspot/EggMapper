---
layout: default
title: API Reference
nav_order: 4
description: "Complete API reference for EggMapper — MapperConfiguration, IMapper, profiles, fluent builders."
---

# API Reference
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Namespace `EggMapper`

---

### `MapperConfiguration`

The root configuration object. Construct once at startup; keep as a singleton.

```csharp
public sealed class MapperConfiguration
```

#### Constructor

```csharp
public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
```

Compiles all registered type maps into cached delegates during construction. This is the only expensive call — all subsequent mapping is near-zero overhead.

**Example:**

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Order, OrderDto>()
        .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName));

    cfg.CreateMap<OrderLine, OrderLineDto>();
    cfg.CreateMap<Customer, CustomerDto>();
    cfg.CreateMap<Address, AddressDto>();

    cfg.AddProfile<ProductProfile>();
});
```

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateMapper()` | `IMapper` | Returns a mapper backed by the compiled cache |
| `AssertConfigurationIsValid()` | `void` | Throws if any destination property is unmapped |
| `BuildProjection<TSrc, TDst>()` | `Expression<Func<TSrc, TDst>>` | Returns the raw projection expression for EF Core / LINQ |

**`CreateMapper()` example:**

```csharp
IMapper mapper = config.CreateMapper();

// mapper is lightweight — backed by the shared compiled cache
// Safe to call multiple times; each returns a new wrapper around the same cache
```

**`AssertConfigurationIsValid()` example:**

```csharp
// In a unit test — catch missing maps before production
[Fact]
public void MappingConfiguration_ShouldBeValid()
{
    var config = new MapperConfiguration(cfg =>
        cfg.AddProfiles(typeof(OrderProfile).Assembly));

    config.AssertConfigurationIsValid(); // Throws on unmapped properties
}
```

**`BuildProjection<TSrc, TDst>()` example:**

```csharp
// Get the raw expression for inspection or composition
Expression<Func<Order, OrderDto>> expr = config.BuildProjection<Order, OrderDto>();

// Use in custom LINQ queries
var results = dbContext.Orders
    .Where(o => o.IsActive)
    .Select(expr)
    .ToListAsync();
```

---

### `IMapper`

The runtime mapping interface. Resolve from DI or call `config.CreateMapper()`.

```csharp
public interface IMapper
```

#### Methods

| Signature | Returns | Description |
|-----------|---------|-------------|
| `Map<TSrc, TDst>(TSrc source)` | `TDst` | Map source to a new destination instance |
| `Map<TSrc, TDst>(TSrc source, TDst destination)` | `TDst` | Map source into an existing destination instance |
| `Map<TDst>(object source)` | `TDst` | Map source (type inferred at runtime) to `TDst` |
| `MapList<TSrc, TDst>(IEnumerable<TSrc> source)` | `List<TDst>` | Batch map a collection using an inlined compiled loop |
| `Patch<TSrc, TDst>(TSrc source, TDst destination)` | `TDst` | Partial update — only non-null properties are copied |

**`Map<TSrc, TDst>(source)` — fastest path (static generic cache):**

```csharp
var dto = mapper.Map<Order, OrderDto>(order);
// Uses FastCache<Order, OrderDto> — zero dictionary lookup after first call
```

**`Map<TSrc, TDst>(source, destination)` — map into existing object:**

```csharp
var existing = await db.Orders.FindAsync(id);
mapper.Map(updateRequest, existing);
// existing is populated in-place, no new allocation
await db.SaveChangesAsync();
```

**`Map<TDst>(object)` — runtime type resolution:**

```csharp
// Useful when source type is not known at compile time
object entity = GetEntityFromSomewhere();
var dto = mapper.Map<OrderDto>(entity);
// Source type resolved via GetType() — falls back to base-type + interface walk
```

The generic `Map<TSrc, TDst>()` overload is faster than `Map<TDst>(object)` because it uses the static generic cache. Prefer the generic version in hot paths.
{: .note }

**`MapList<TSrc, TDst>(source)` — batch collection mapping:**

```csharp
var orders = await db.Orders.ToListAsync();
List<OrderDto> dtos = mapper.MapList<Order, OrderDto>(orders);
// Entire loop compiled as a single expression tree — near-manual speed
```

**`Patch<TSrc, TDst>(source, destination)` — partial update:**

```csharp
// API endpoint for PATCH /products/{id}
app.MapPatch("/products/{id}", async (int id, UpdateProductRequest req, AppDbContext db, IMapper mapper) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    mapper.Patch(req, product); // Only non-null fields are written
    await db.SaveChangesAsync();
    return Results.NoContent();
});
```

---

### `IMapperConfigurationExpression`

Fluent interface passed to the `MapperConfiguration` constructor callback.

| Method | Returns | Description |
|--------|---------|-------------|
| `CreateMap<TSrc, TDst>()` | `IMappingExpression<TSrc, TDst>` | Register a type map |
| `CreateMap(Type src, Type dst)` | `IMappingExpression` | Register an open-generic type map |
| `AddProfile<TProfile>()` | `void` | Register all maps in a `Profile` subclass |
| `AddProfile(Profile profile)` | `void` | Register a pre-constructed `Profile` instance |
| `AddProfiles(params Assembly[] assemblies)` | `void` | Scan assemblies and register all `Profile` subclasses |

**`CreateMap<TSrc, TDst>()` example:**

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Customer, CustomerDto>();
    cfg.CreateMap<Address, AddressDto>();
});
```

**`CreateMap(Type, Type)` — open generics:**

```csharp
var config = new MapperConfiguration(cfg =>
{
    // Maps Result<T> to ResultDto<T> for any T
    cfg.CreateMap(typeof(Result<>), typeof(ResultDto<>));
});

// Usage
var result = new Result<Order> { Data = order, Success = true };
var dto = mapper.Map<Result<Order>, ResultDto<Order>>(result);
```

**`AddProfiles(assemblies)` example:**

```csharp
var config = new MapperConfiguration(cfg =>
    cfg.AddProfiles(
        typeof(OrderProfile).Assembly,
        typeof(ReportProfile).Assembly));
```

---

### `IMappingExpression<TSrc, TDst>`

Fluent builder returned by `CreateMap<TSrc, TDst>()`.

| Method | Description |
|--------|-------------|
| `ForMember(dst => dst.Prop, opt => opt.MapFrom(...))` | Custom source expression |
| `ForMember(dst => dst.Prop, opt => opt.Ignore())` | Skip destination property |
| `ForMember(dst => dst.Prop, opt => opt.Condition(s => ...))` | Map only when predicate is true |
| `ForMember(dst => dst.Prop, opt => opt.PreCondition(s => ...))` | Skip source read when predicate is false |
| `ForMember(dst => dst.Prop, opt => opt.NullSubstitute(value))` | Use fallback when source is null |
| `ForPath(dst => dst.A.B.Prop, opt => opt.MapFrom(...))` | Map to a deeply nested destination path |
| `ReverseMap()` | Also register the inverse mapping |
| `BeforeMap(Action<TSrc, TDst>)` | Hook called before any property is mapped |
| `AfterMap(Action<TSrc, TDst>)` | Hook called after all properties are mapped |
| `MaxDepth(int depth)` | Limit recursion depth for self-referencing types |
| `IncludeBase<TBaseSrc, TBaseDst>()` | Inherit the base type's mapping rules |
| `Validate(dst => dst.Prop, predicate, message)` | Post-mapping validation rule |

**`ForMember` with `MapFrom` — custom mapping expression:**

```csharp
cfg.CreateMap<Order, OrderDto>()
    .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName))
    .ForMember(d => d.TotalWithTax, o => o.MapFrom(s => s.Total * 1.1m))
    .ForMember(d => d.Status, o => o.MapFrom(s => s.Status.ToString()));
```

**`ForMember` with `Ignore` — exclude sensitive data:**

```csharp
cfg.CreateMap<User, UserDto>()
    .ForMember(d => d.PasswordHash, o => o.Ignore())
    .ForMember(d => d.SecurityStamp, o => o.Ignore())
    .ForMember(d => d.TwoFactorSecret, o => o.Ignore());
```

**`ForMember` with `Condition` — conditional mapping:**

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.DiscountPrice,
               o => o.Condition(s => s.Discount > 0))
    .ForMember(d => d.WarehouseLocation,
               o => o.Condition((src, dst) => src.Stock > 0));
```

**`ForMember` with `NullSubstitute` — fallback values:**

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.Description, o => o.NullSubstitute("No description available"))
    .ForMember(d => d.ImageUrl, o => o.NullSubstitute("/images/placeholder.png"));
```

**`ForPath` — nested destination paths:**

```csharp
cfg.CreateMap<OrderFlatDto, Order>()
    .ForPath(d => d.Customer.Name, o => o.MapFrom(s => s.CustomerName))
    .ForPath(d => d.Customer.Address.City, o => o.MapFrom(s => s.ShippingCity))
    .ForPath(d => d.Customer.Address.PostalCode, o => o.MapFrom(s => s.ShippingZip));
```

**`ReverseMap` — bidirectional mapping:**

```csharp
cfg.CreateMap<Product, ProductDto>().ReverseMap();
// Equivalent to:
// cfg.CreateMap<Product, ProductDto>();
// cfg.CreateMap<ProductDto, Product>();
```

**`BeforeMap` / `AfterMap` — lifecycle hooks:**

```csharp
cfg.CreateMap<Order, OrderDto>()
    .BeforeMap((src, dst) =>
    {
        // Normalize data before mapping
        src.CustomerName = src.CustomerName?.Trim();
    })
    .AfterMap((src, dst) =>
    {
        dst.MappedAt = DateTime.UtcNow;
        dst.DisplayId = $"ORD-{dst.Id:D6}";
    });
```

**`MaxDepth` — self-referencing types:**

```csharp
// Category has Children: List<Category> (tree structure)
cfg.CreateMap<Category, CategoryDto>()
    .MaxDepth(3);
// Maps 3 levels deep, then stops (children beyond depth 3 are null)
```

**`IncludeBase` — polymorphic inheritance:**

```csharp
cfg.CreateMap<Vehicle, VehicleDto>();
cfg.CreateMap<Car, CarDto>().IncludeBase<Vehicle, VehicleDto>();
cfg.CreateMap<Truck, TruckDto>().IncludeBase<Vehicle, VehicleDto>();

// When mapping a Car, base Vehicle properties are mapped via the base map
var car = new Car { Make = "Toyota", Doors = 4 };
var dto = mapper.Map<Car, CarDto>(car);
// dto.Make == "Toyota" (from Vehicle map), dto.Doors == 4 (from Car map)
```

**`Validate` — post-mapping validation:**

```csharp
cfg.CreateMap<CreateOrderRequest, Order>()
    .Validate(d => d.CustomerName, n => !string.IsNullOrWhiteSpace(n), "Customer name is required")
    .Validate(d => d.Total, t => t > 0, "Order total must be positive")
    .Validate(d => d.Lines, l => l.Count > 0, "Order must have at least one line");
```

---

### `Profile`

Base class for grouping related maps.

```csharp
public abstract class Profile
```

Call `CreateMap<TSrc, TDst>()` inside your constructor to register maps. The API is identical to `IMapperConfigurationExpression`.

**Example — real-world profile:**

```csharp
public class ECommerceProfile : Profile
{
    public ECommerceProfile()
    {
        // Order aggregate
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.Customer.FullName))
            .ForMember(d => d.ShippingCity, o => o.MapFrom(s => s.ShippingAddress.City));

        CreateMap<Order, OrderSummaryDto>()
            .ForMember(d => d.LineCount, o => o.MapFrom(s => s.Lines.Count));

        CreateMap<OrderLine, OrderLineDto>();

        // Customer aggregate
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.PasswordHash, o => o.Ignore());

        CreateMap<Address, AddressDto>();

        // Reverse maps for write operations
        CreateMap<CreateOrderRequest, Order>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.CreatedAt, o => o.Ignore());
    }
}
```

---

### `MappingException`

Thrown when a mapping fails at runtime (e.g. unsupported type conversion, null reference in a non-nullable path).

```csharp
public sealed class MappingException : Exception
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `SourceType` | `Type` | The source type being mapped |
| `DestinationType` | `Type` | The destination type |
| `InnerException` | `Exception?` | The underlying exception |

**Handling example:**

```csharp
try
{
    var dto = mapper.Map<Order, OrderDto>(order);
}
catch (MappingException ex)
{
    logger.LogError(ex,
        "Mapping failed: {Source} -> {Dest}",
        ex.SourceType.Name,
        ex.DestinationType.Name);
}
```

---

### `MappingValidationException`

Thrown when post-mapping validation rules fail.

```csharp
public sealed class MappingValidationException : Exception
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Errors` | `IReadOnlyList<string>` | All validation messages that failed |

**Handling example:**

```csharp
try
{
    var order = mapper.Map<CreateOrderRequest, Order>(request);
}
catch (MappingValidationException ex)
{
    // Return all errors to the client
    return Results.ValidationProblem(
        ex.Errors.ToDictionary(
            e => "mapping",
            e => new[] { e }));
}
```

---

## Extension Methods

### `QueryableExtensions.ProjectTo<TSrc, TDst>()`

```csharp
public static IQueryable<TDst> ProjectTo<TSrc, TDst>(
    this IQueryable<TSrc> source,
    MapperConfiguration config)
```

Builds a pure expression tree from the registered type map and passes it to `IQueryable.Select()`. The expression is never compiled by EggMapper — the LINQ provider translates it.

**Example:**

```csharp
// All of these work with ProjectTo
var activeProducts = await dbContext.Products
    .Where(p => p.IsActive)
    .ProjectTo<Product, ProductDto>(config)
    .OrderBy(d => d.Name)
    .ToListAsync();

// With pagination
var page = await dbContext.Orders
    .Where(o => o.CustomerId == customerId)
    .OrderByDescending(o => o.CreatedAt)
    .ProjectTo<Order, OrderSummaryDto>(config)
    .Skip(pageSize * pageIndex)
    .Take(pageSize)
    .ToListAsync();
```

---

## Namespace `Microsoft.Extensions.DependencyInjection`

### `EggMapperServiceCollectionExtensions`

| Extension method | Description |
|------------------|-------------|
| `AddEggMapper(params Assembly[])` | Scan assemblies for `Profile` subclasses and register `IMapper` as singleton |
| `AddEggMapper(Action<IMapperConfigurationExpression>)` | Inline configuration without profiles |

**Assembly scanning:**

```csharp
builder.Services.AddEggMapper(typeof(OrderProfile).Assembly);
```

**Inline configuration:**

```csharp
builder.Services.AddEggMapper(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
    cfg.CreateMap<Customer, CustomerDto>();
});
```

**Multiple assemblies:**

```csharp
builder.Services.AddEggMapper(
    typeof(OrderProfile).Assembly,       // Web API layer
    typeof(ReportProfile).Assembly);     // Reporting layer
```

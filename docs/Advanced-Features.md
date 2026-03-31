---
layout: default
title: Advanced Features
parent: Guide
nav_order: 4
description: "Advanced EggMapper features — ForMember, conditions, hooks, ProjectTo, patch mapping, validation, open generics."
---

# Advanced Features
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## `ForMember` — Custom Member Mapping

Override the default convention for any destination property:

```csharp
cfg.CreateMap<Customer, CustomerDto>()
    .ForMember(d => d.FullName,
               opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
    .ForMember(d => d.City,
               opt => opt.MapFrom(s => s.Address.City));
```

### Map from a computed expression

```csharp
cfg.CreateMap<Order, OrderDto>()
    .ForMember(d => d.DisplayPrice,
               opt => opt.MapFrom(s => $"{s.Currency} {s.Price:F2}"))
    .ForMember(d => d.TotalWithTax,
               opt => opt.MapFrom(s => s.Total * (1 + s.TaxRate)))
    .ForMember(d => d.LineCount,
               opt => opt.MapFrom(s => s.Lines.Count));
```

### Map from a deeply nested source

```csharp
cfg.CreateMap<Order, OrderFlatDto>()
    .ForMember(d => d.CustomerName,
               opt => opt.MapFrom(s => s.Customer.FullName))
    .ForMember(d => d.CustomerEmail,
               opt => opt.MapFrom(s => s.Customer.Email))
    .ForMember(d => d.ShippingCity,
               opt => opt.MapFrom(s => s.ShippingAddress.City))
    .ForMember(d => d.ShippingCountry,
               opt => opt.MapFrom(s => s.ShippingAddress.Country));
```

---

## `Ignore()` — Skip a Property

Tell EggMapper to leave a destination property at its default value:

```csharp
cfg.CreateMap<User, UserDto>()
    .ForMember(d => d.PasswordHash, opt => opt.Ignore())
    .ForMember(d => d.InternalId,   opt => opt.Ignore())
    .ForMember(d => d.SecurityStamp, opt => opt.Ignore());
```

Use `Ignore()` for sensitive data (passwords, tokens, internal IDs) or computed-only properties that should not come from the source.
{: .note }

---

## `ReverseMap()` — Bidirectional Mapping

Register the inverse mapping in one call:

```csharp
cfg.CreateMap<Order, OrderDto>().ReverseMap();
// Registers: Order -> OrderDto  AND  OrderDto -> Order
```

### Real-world use: API request/response symmetry

```csharp
cfg.CreateMap<Product, ProductDto>().ReverseMap();

// Read: Entity -> DTO
var dto = mapper.Map<Product, ProductDto>(product);

// Write: DTO -> Entity (for create/update endpoints)
var entity = mapper.Map<ProductDto, Product>(dto);
```

---

## `ForPath` — Map to a Nested Destination Property

Write to a property deep in the destination object graph:

```csharp
cfg.CreateMap<OrderFlatDto, Order>()
    .ForPath(d => d.Customer.Name,
             opt => opt.MapFrom(s => s.CustomerName))
    .ForPath(d => d.Customer.Address.City,
             opt => opt.MapFrom(s => s.ShippingCity))
    .ForPath(d => d.Customer.Address.PostalCode,
             opt => opt.MapFrom(s => s.ShippingZip));
```

EggMapper creates intermediate objects (`Customer`, `Address`) automatically when using `ForPath`.
{: .note }

---

## Nested Object Mapping

Declare maps for nested types and EggMapper uses them automatically:

```csharp
cfg.CreateMap<Address,  AddressDto>();
cfg.CreateMap<Customer, CustomerDto>();
// CustomerDto.Address is mapped via the Address -> AddressDto map
```

### Real-world example: Order with nested Customer and Address

```csharp
// Entities
public class Order
{
    public int Id { get; set; }
    public Customer Customer { get; set; } = null!;
    public Address ShippingAddress { get; set; } = null!;
    public List<OrderLine> Lines { get; set; } = [];
}

// DTOs
public class OrderDto
{
    public int Id { get; set; }
    public CustomerDto Customer { get; set; } = null!;
    public AddressDto ShippingAddress { get; set; } = null!;
    public List<OrderLineDto> Lines { get; set; } = [];
}

// Configuration — register maps for each level
cfg.CreateMap<Order, OrderDto>();
cfg.CreateMap<Customer, CustomerDto>();
cfg.CreateMap<Address, AddressDto>();
cfg.CreateMap<OrderLine, OrderLineDto>();

// Map — nested objects and collections handled automatically
var dto = mapper.Map<Order, OrderDto>(order);
// dto.Customer.Name, dto.ShippingAddress.City, dto.Lines[0].ProductName all populated
```

If you forget to register a nested type map, `AssertConfigurationIsValid()` will catch it. Always call it in your test suite.
{: .warning }

---

## Collection Mapping

Supported collection types out of the box:

| Source | Destination |
|--------|-------------|
| `T[]` | `T[]`, `List<T>`, `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `HashSet<T>` |
| `List<T>` | All of the above |
| `IEnumerable<T>` | All of the above |

```csharp
cfg.CreateMap<Order,    OrderDto>();
cfg.CreateMap<Customer, CustomerDto>();

// CustomerDto.Orders (List<OrderDto>) mapped automatically from Customer.Orders (List<Order>)
```

### Batch mapping with `MapList`

```csharp
var orders = await db.Orders.ToListAsync();

// Fully inlined compiled loop — near-manual speed
List<OrderDto> dtos = mapper.MapList<Order, OrderDto>(orders);
```

### Array mapping

```csharp
cfg.CreateMap<Product, ProductDto>();

Product[] products = GetProducts();
var dtos = mapper.Map<List<ProductDto>>(products);
// Also: mapper.Map<ProductDto[]>(products)
```

---

## Conditional Mapping

### `Condition` — skip if a value-level predicate fails

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.DiscountPrice,
               opt => opt.Condition(s => s.Discount > 0));
// DiscountPrice only set when there is actually a discount
```

### `PreCondition` — skip the source read entirely

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.WarehouseCode,
               opt => opt.PreCondition(s => s.IsPhysical));
// WarehouseCode not even read from source for digital products
```

### Full condition (source + destination)

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.Price,
               opt => opt.Condition((src, dst) => src.Price != dst.Price));
// Only update price if it actually changed — useful with Map(src, existingDst)
```

### Real-world example: conditional mapping for API responses

```csharp
cfg.CreateMap<User, UserProfileDto>()
    .ForMember(d => d.Email,
               opt => opt.Condition(s => s.EmailVerified))
    .ForMember(d => d.PhoneNumber,
               opt => opt.Condition(s => s.PhoneVerified))
    .ForMember(d => d.AdminNotes,
               opt => opt.PreCondition(s => s.Role == UserRole.Admin));
```

---

## Null Substitution

Provide a fallback value when the source property is `null`:

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.Description,
               opt => opt.NullSubstitute("No description available"))
    .ForMember(d => d.ImageUrl,
               opt => opt.NullSubstitute("/images/placeholder.png"))
    .ForMember(d => d.Category,
               opt => opt.NullSubstitute("Uncategorized"));
```

---

## Before / After Map Hooks

Run custom logic immediately before or after the mapping:

```csharp
cfg.CreateMap<Order, OrderDto>()
    .BeforeMap((src, dst) =>
    {
        // Normalize or validate source data
        src.CustomerName = src.CustomerName?.Trim() ?? "";
    })
    .AfterMap((src, dst) =>
    {
        // Compute derived fields after mapping
        dst.MappedAt = DateTime.UtcNow;
        dst.DisplayId = $"ORD-{dst.Id:D6}";
    });
```

### Use case: auditing mapped objects

```csharp
cfg.CreateMap<Order, OrderAuditDto>()
    .AfterMap((src, dst) =>
    {
        dst.AuditTimestamp = DateTimeOffset.UtcNow;
        dst.AuditSource = "OrderService";
        dst.ChangeHash = ComputeHash(dst);
    });
```

Maps that use `BeforeMap` / `AfterMap` take the flexible delegate path, which is slightly slower than the context-free path used by simple maps. Only use hooks when you need them.
{: .note }

---

## MaxDepth — Self-Referencing / Circular Types

Prevent infinite recursion on types that reference themselves:

```csharp
cfg.CreateMap<Category, CategoryDto>()
    .MaxDepth(3);
// Category.Children -> CategoryDto.Children mapped up to depth 3
// Beyond depth 3, Children is null
```

### Real-world example: organizational hierarchy

```csharp
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public Department? Parent { get; set; }
    public List<Department> SubDepartments { get; set; } = [];
}

cfg.CreateMap<Department, DepartmentDto>()
    .MaxDepth(5); // Org chart depth limit
```

### Real-world example: threaded comments

```csharp
public class Comment
{
    public int Id { get; set; }
    public string Body { get; set; } = "";
    public List<Comment> Replies { get; set; } = [];
}

cfg.CreateMap<Comment, CommentDto>()
    .MaxDepth(10); // Limit nesting depth for threaded discussions
```

---

## Inheritance / Include

Map a derived type through the base type map:

```csharp
cfg.CreateMap<Vehicle, VehicleDto>();
cfg.CreateMap<Car, CarDto>().IncludeBase<Vehicle, VehicleDto>();
cfg.CreateMap<Truck, TruckDto>().IncludeBase<Vehicle, VehicleDto>();
```

### Real-world example: EF Core TPH (Table Per Hierarchy)

```csharp
// EF Core entities using TPH inheritance
public abstract class Payment
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class CreditCardPayment : Payment
{
    public string Last4Digits { get; set; } = "";
    public string CardBrand { get; set; } = "";
}

public class BankTransferPayment : Payment
{
    public string BankName { get; set; } = "";
    public string ReferenceNumber { get; set; } = "";
}

// DTOs
public class PaymentDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = "";
}

public class CreditCardPaymentDto : PaymentDto
{
    public string Last4Digits { get; set; } = "";
    public string CardBrand { get; set; } = "";
}

public class BankTransferPaymentDto : PaymentDto
{
    public string BankName { get; set; } = "";
}

// Configuration
cfg.CreateMap<Payment, PaymentDto>()
    .ForMember(d => d.Type, o => o.MapFrom(s => s.GetType().Name));

cfg.CreateMap<CreditCardPayment, CreditCardPaymentDto>()
    .IncludeBase<Payment, PaymentDto>();

cfg.CreateMap<BankTransferPayment, BankTransferPaymentDto>()
    .IncludeBase<Payment, PaymentDto>();
```

---

## Enum Mapping

Enums are mapped by value (numeric) by default. Properties with identical enum types are copied directly.

```csharp
public enum OrderStatus { Pending, Processing, Shipped, Delivered }
public enum OrderStatusDto { Pending, Processing, Shipped, Delivered }

cfg.CreateMap<Order, OrderDto>();
// Order.Status (OrderStatus) -> OrderDto.Status (OrderStatusDto) by numeric value
```

### Enum to string

```csharp
cfg.CreateMap<Order, OrderDto>()
    .ForMember(d => d.StatusText, o => o.MapFrom(s => s.Status.ToString()));
```

---

## Constructor Mapping

If the destination has a constructor whose parameter names match source property names, EggMapper uses it automatically:

```csharp
public record OrderDto(int Id, string CustomerName, decimal Total);

cfg.CreateMap<Order, OrderDto>();
// Uses the positional constructor: new OrderDto(src.Id, src.CustomerName, src.Total)
```

### Record types with additional properties

```csharp
public record ProductDto(int Id, string Name)
{
    public decimal Price { get; init; }
    public string Category { get; init; } = "";
}

cfg.CreateMap<Product, ProductDto>();
// Constructor: new ProductDto(src.Id, src.Name) { Price = src.Price, Category = src.Category }
```

### Immutable value objects

```csharp
public class Money
{
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }
}

cfg.CreateMap<PriceEntity, Money>();
// Calls: new Money(src.Amount, src.Currency)
```

EggMapper scores constructors by how many parameter names match source property names (case-insensitive) and picks the highest-scoring one.
{: .note }

---

## Patch / Partial Mapping

`mapper.Patch<TSource, TDestination>(source, destination)` copies only the *set* properties from `source` onto an existing `destination` object:

- **Reference types** (`string`, classes) — copied only when the source value is non-null
- **`Nullable<T>`** — copied only when `.HasValue` is true
- **Non-nullable value types** (`int`, `bool`, etc.) — always copied (no sentinel for "not set")

```csharp
cfg.CreateMap<UpdateOrderRequest, Order>();

var existing = db.Orders.Find(id)!;
mapper.Patch(request, existing);   // only non-null fields overwrite existing
db.SaveChanges();
```

No extra configuration needed — every type map automatically gets a patch delegate compiled at startup.

### Real-world example: PATCH endpoint

```csharp
public class UpdateProductRequest
{
    public string? Name { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public int? CategoryId { get; set; }
}

// Only the fields the client sends are non-null
// PATCH /products/42  { "name": "New Name", "price": 29.99 }

app.MapPatch("/products/{id}", async (int id, UpdateProductRequest req, AppDbContext db, IMapper mapper) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    mapper.Patch(req, product);
    // product.Name = "New Name", product.Price = 29.99
    // product.Description and product.CategoryId unchanged

    await db.SaveChangesAsync();
    return Results.NoContent();
});
```

---

## Inline Validation

Add post-mapping validation rules directly to a type map with `.Validate()`. All rules run after mapping completes; a `MappingValidationException` is thrown that contains **every** violation (not just the first):

```csharp
cfg.CreateMap<CreateOrderRequest, Order>()
    .Validate(d => d.CustomerName, n => !string.IsNullOrWhiteSpace(n), "Customer name is required")
    .Validate(d => d.Total, t => t > 0, "Order total must be positive")
    .Validate(d => d.Lines, l => l.Count > 0, "Order must have at least one line");
```

```csharp
try
{
    var order = mapper.Map<CreateOrderRequest, Order>(request);
}
catch (MappingValidationException ex)
{
    // ex.Errors — IReadOnlyList<string> with all violations
    foreach (var err in ex.Errors)
        Console.WriteLine(err);
}
```

### Use in a minimal API endpoint

```csharp
app.MapPost("/orders", (CreateOrderRequest req, IMapper mapper, AppDbContext db) =>
{
    try
    {
        var order = mapper.Map<CreateOrderRequest, Order>(req);
        db.Orders.Add(order);
        db.SaveChanges();
        return Results.Created($"/orders/{order.Id}", order);
    }
    catch (MappingValidationException ex)
    {
        return Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["mapping"] = [.. ex.Errors]
            });
    }
});
```

Maps without `.Validate()` calls use the zero-overhead context-free path. There is no performance penalty for the common case.
{: .note }

---

## IQueryable Projection (ProjectTo)

`ProjectTo<TSource, TDest>(config)` builds a pure `Expression<Func<TSource, TDest>>` from the registered type map and passes it directly to `IQueryable.Select()`. The expression is **never compiled** by EggMapper, so LINQ providers (EF Core, etc.) can translate it to SQL.

```csharp
cfg.CreateMap<Order, OrderDto>();

// EF Core — translated to SQL SELECT
var dtos = await dbContext.Orders
    .Where(o => o.IsActive)
    .ProjectTo<Order, OrderDto>(config)
    .ToListAsync();
```

Supports:
- Flat DTOs (`MemberInitExpression`)
- Records and parameterized constructors (`NewExpression` with member associations)
- Nested registered maps (recursive projection)
- Flattened properties (`AddressStreet` -> `src.Address.Street`)
- Custom `MapFrom` expressions inlined into the projection tree

### Complex query with ProjectTo

```csharp
// Paginated order list with filtering
var page = await dbContext.Orders
    .Where(o => o.CustomerId == customerId)
    .Where(o => o.Status != OrderStatus.Cancelled)
    .OrderByDescending(o => o.CreatedAt)
    .ProjectTo<Order, OrderSummaryDto>(config)
    .Skip(pageSize * pageIndex)
    .Take(pageSize)
    .ToListAsync();
```

### ProjectTo with nested maps

```csharp
cfg.CreateMap<Order, OrderDetailDto>();
cfg.CreateMap<Customer, CustomerBriefDto>();
cfg.CreateMap<Address, AddressBriefDto>();

// EF Core generates a single SQL query with JOINs
var detail = await dbContext.Orders
    .Where(o => o.Id == orderId)
    .ProjectTo<Order, OrderDetailDto>(config)
    .FirstOrDefaultAsync();
// detail.Customer and detail.ShippingAddress are populated from SQL, not in memory
```

### Get the raw expression

```csharp
Expression<Func<Order, OrderDto>> expr = config.BuildProjection<Order, OrderDto>();

// Compose with other expressions
var combined = dbContext.Orders
    .Where(o => o.IsActive)
    .Select(expr);
```

`ProjectTo` eliminates N+1 queries. Instead of loading entities and mapping in memory, the entire projection becomes a single SQL query. Always prefer `ProjectTo` when reading data you do not need to modify.
{: .note }

---

## Open Generic Mapping

Map generic wrapper types without registering every closed variant:

```csharp
// Generic wrapper types
public class Result<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

public class ResultDto<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
}

// Register the open generic map once
cfg.CreateMap(typeof(Result<>), typeof(ResultDto<>));

// Works for any T
var orderResult = new Result<Order> { Success = true, Data = order };
var dto = mapper.Map<Result<Order>, ResultDto<OrderDto>>(orderResult);
// dto.Success == true, dto.Data is mapped via Order -> OrderDto
```

### Real-world example: paginated API responses

```csharp
public class PagedResult<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
}

cfg.CreateMap(typeof(PagedResult<>), typeof(PagedResultDto<>));
cfg.CreateMap<Product, ProductDto>();

var pagedProducts = new PagedResult<Product> { Items = products, TotalCount = 150 };
var dto = mapper.Map<PagedResult<Product>, PagedResultDto<ProductDto>>(pagedProducts);
```

---

## Same-Type Mapping (Cloning)

Map an object to the same type without any configuration:

```csharp
// No CreateMap<Customer, Customer>() needed
var copy = mapper.Map<Customer, Customer>(customer);
```

This is useful for creating snapshots before mutation, or for detaching EF Core tracked entities.

```csharp
// Create a detached copy for comparison
var before = mapper.Map<Order, Order>(order);

// Mutate the tracked entity
order.Status = OrderStatus.Shipped;
order.ShippedAt = DateTime.UtcNow;

// Compare
if (before.Status != order.Status)
    await PublishOrderStatusChanged(order);
```

---

## Configuration Validation

Validate at startup (or in tests) that every destination property is covered:

```csharp
config.AssertConfigurationIsValid();
// Throws if any destination property is unmapped and not ignored
```

### Recommended: validate in a unit test

```csharp
[Fact]
public void AllMappings_ShouldBeValid()
{
    var config = new MapperConfiguration(cfg =>
        cfg.AddProfiles(typeof(OrderProfile).Assembly));

    config.AssertConfigurationIsValid();
}
```

---

## Common Pitfalls

Missing nested type maps are the most common source of bugs. If `OrderDto.Customer` is a `CustomerDto`, you need `CreateMap<Customer, CustomerDto>()` in addition to `CreateMap<Order, OrderDto>()`.
{: .warning }

- **Missing nested type maps** — Register maps for every nested type in your object graph. Use `AssertConfigurationIsValid()` to catch these.
- **Circular references without `MaxDepth`** — Self-referencing types (trees, graphs) will cause a stack overflow without `MaxDepth()`.
- **Using `BeforeMap`/`AfterMap` unnecessarily** — These hooks force the flexible delegate path. For simple computed properties, use `MapFrom` instead.
- **Forgetting to register `Ignore()` for validation** — `AssertConfigurationIsValid()` will fail on unmapped destination properties. Use `Ignore()` for properties you intentionally leave unmapped.
- **Registering maps after construction** — `MapperConfiguration` is immutable after construction. All maps must be registered in the constructor callback.

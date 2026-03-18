# Advanced Features

## `ForMember` — Custom Member Mapping

Override the default convention for any destination property:

```csharp
cfg.CreateMap<Customer, CustomerDto>()
    .ForMember(d => d.FullName,
               opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
    .ForMember(d => d.City,
               opt => opt.MapFrom(s => s.Address.City));
```

### Map from a custom resolver function

```csharp
.ForMember(d => d.DisplayPrice,
           opt => opt.MapFrom(s => $"{s.Currency} {s.Price:F2}"))
```

---

## `Ignore()` — Skip a Property

Tell EggMapper to leave a destination property at its default value:

```csharp
cfg.CreateMap<User, UserDto>()
    .ForMember(d => d.PasswordHash, opt => opt.Ignore())
    .ForMember(d => d.InternalId,   opt => opt.Ignore());
```

---

## `ReverseMap()` — Bidirectional Mapping

Register the inverse mapping in one call:

```csharp
cfg.CreateMap<Order, OrderDto>().ReverseMap();
// Registers: Order → OrderDto  AND  OrderDto → Order
```

---

## `ForPath` — Map to a Nested Destination Property

Write to a property deep in the destination object graph:

```csharp
cfg.CreateMap<OrderDto, Order>()
    .ForPath(d => d.Customer.Address.City,
             opt => opt.MapFrom(s => s.ShippingCity));
```

---

## Nested Object Mapping

Declare maps for nested types and EggMapper uses them automatically:

```csharp
cfg.CreateMap<Address,  AddressDto>();
cfg.CreateMap<Customer, CustomerDto>();
// CustomerDto.Address is mapped via the Address → AddressDto map
```

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

// CustomerDto.Orders (List<OrderDto>) is mapped automatically from Customer.Orders (List<Order>)
```

---

## Conditional Mapping

### `Condition` — skip if a value-level predicate fails

```csharp
cfg.CreateMap<Source, Destination>()
    .ForMember(d => d.Price,
               opt => opt.Condition(s => s.Price > 0));
```

### `PreCondition` — skip the source read entirely

```csharp
.ForMember(d => d.Price,
           opt => opt.PreCondition(s => s.IsActive))
```

### Full condition (source + destination)

```csharp
.ForMember(d => d.Value,
           opt => opt.Condition((src, dst) => src.Value != dst.Value))
```

---

## Null Substitution

Provide a fallback value when the source property is `null`:

```csharp
cfg.CreateMap<Product, ProductDto>()
    .ForMember(d => d.Description,
               opt => opt.NullSubstitute("N/A"));
```

---

## Before / After Map Hooks

Run custom logic immediately before or after the mapping:

```csharp
cfg.CreateMap<Order, OrderDto>()
    .BeforeMap((src, dst) => Console.WriteLine($"Mapping order {src.Id}"))
    .AfterMap((src, dst)  => dst.MappedAt = DateTime.UtcNow);
```

---

## MaxDepth — Self-Referencing / Circular Types

Prevent infinite recursion on types that reference themselves:

```csharp
cfg.CreateMap<Category, CategoryDto>()
    .MaxDepth(3);
// Category.Children → CategoryDto.Children is mapped up to depth 3
```

---

## Inheritance / Include

Map a derived type through the base type map:

```csharp
cfg.CreateMap<Animal,   AnimalDto>();
cfg.CreateMap<Dog,      DogDto>().IncludeBase<Animal, AnimalDto>();
cfg.CreateMap<Cat,      CatDto>().IncludeBase<Animal, AnimalDto>();
```

---

## Enum Mapping

Enums are mapped by value (numeric) by default. Properties with identical enum types are copied directly.

```csharp
public enum Status { Active, Inactive }
public enum StatusDto { Active, Inactive }

cfg.CreateMap<Order, OrderDto>();
// Order.Status (Status) → OrderDto.Status (StatusDto) by numeric value
```

---

## Constructor Mapping

If the destination has a constructor whose parameter names match source property names, EggMapper uses it automatically:

```csharp
public record OrderDto(int Id, string CustomerName, decimal Total);

cfg.CreateMap<Order, OrderDto>();
// Uses the positional constructor
```

---

## Configuration Validation

Validate at startup (or in tests) that every destination property is covered:

```csharp
config.AssertConfigurationIsValid();
// Throws if any destination property is unmapped and not ignored
```

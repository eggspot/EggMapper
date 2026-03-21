# Getting Started: Tier 2 — Compile-Time Extension Methods (`[MapTo]`)

`EggMapper.Generator` generates **zero-reflection, zero-allocation extension methods** at build time from a single `[MapTo]` attribute.  No `MapperConfiguration`, no runtime delegates — the mapping code is emitted directly into your binary.

---

## Installation

```bash
dotnet add package EggMapper.Generator
```

> The generator package automatically injects the `[MapTo]`, `[MapProperty]`, and `[MapIgnore]` attributes into your compilation — no separate abstractions package needed.

---

## Basic Usage

### 1 — Annotate the source class

```csharp
using EggMapper;

[MapTo(typeof(OrderDto))]
public class Order
{
    public int    Id           { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Total       { get; set; }
}

public class OrderDto
{
    public int    Id           { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal Total       { get; set; }
}
```

### 2 — Use the generated extension method

```csharp
var order = new Order { Id = 1, CustomerName = "Alice", Total = 99.99m };
OrderDto dto = order.ToOrderDto();               // generated extension method
List<OrderDto> dtos = orders.ToOrderDtoList();   // generated list method
```

No mapper instance, no DI registration, no startup cost.

---

## Multiple Destinations

Apply `[MapTo]` multiple times to map a single source to several destinations:

```csharp
[MapTo(typeof(OrderDto))]
[MapTo(typeof(OrderSummary))]
public class Order { ... }
```

Generates `ToOrderDto()`, `ToOrderDtoList()`, `ToOrderSummary()`, and `ToOrderSummaryList()`.

---

## Renaming Properties (`[MapProperty]`)

```csharp
[MapTo(typeof(CustomerDto))]
public class Customer
{
    public int    Id   { get; set; }

    [MapProperty("FullName")]   // maps Customer.Name → CustomerDto.FullName
    public string Name { get; set; } = "";
}
```

---

## Ignoring Properties (`[MapIgnore]`)

```csharp
[MapTo(typeof(CustomerDto))]
public class Customer
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = "";

    [MapIgnore]
    public string Password { get; set; } = "";  // not copied to CustomerDto
}
```

---

## Post-Map Hook (`AfterMap`)

Declare a partial static method to run custom logic after the generated initializer:

```csharp
// In a separate file (user-authored):
public static partial class OrderToOrderDtoExtensions
{
    static partial void AfterMap(Order source, OrderDto dest)
    {
        dest.DisplayName = $"#{source.Id} — {source.CustomerName}";
    }
}
```

The generated `ToOrderDto()` calls `AfterMap(source, dest)` after building the object.

---

## Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| EGG2001 | Error | A destination property has no matching source member |
| EGG2002 | Info | An implicit type conversion was applied |

---

## When to Use Tier 2 vs Runtime EggMapper

| Scenario | Recommendation |
|----------|---------------|
| Simple 1:1 property copying, no custom logic | **Tier 2** — zero overhead, compile-time safety |
| Complex conditional mapping, `BeforeMap`/`AfterMap` hooks | **Runtime EggMapper** |
| Need `ForMember`, `ConvertUsing`, `MaxDepth` | **Runtime EggMapper** |
| Want type errors at build time, not runtime | **Tier 2** |

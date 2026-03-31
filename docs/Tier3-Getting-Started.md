---
layout: default
title: Class Mapper ([EggMapper])
parent: Code Generation
nav_order: 2
description: "EggMapper.ClassMapper — partial class mapper generation with [EggMapper] attribute."
---

# Class Mapper (`[EggMapper]`)

`EggMapper.ClassMapper` generates implementations for **partial mapping methods** you declare in a class.  You get full IDE auto-complete, type-checked mapping, and the ability to add custom logic alongside the generated code — all at zero runtime cost.

---

## Installation

```bash
dotnet add package EggMapper.ClassMapper
```

---

## Basic Usage

### 1 — Declare a partial mapper class

```csharp
using EggMapper;

[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
}
```

### 2 — The generator produces the implementation

```csharp
// Auto-generated (OrderMapper.g.cs):
public partial class OrderMapper
{
    public static OrderMapper Instance { get; } = new OrderMapper();

    public partial OrderDto Map(Order source)
    {
        if (source is null) throw new ArgumentNullException(nameof(source));
        return new OrderDto
        {
            Id           = source.Id,
            CustomerName = source.CustomerName,
            Total        = source.Total,
        };
    }
}
```

### 3 — Use the mapper

```csharp
// Option A: static singleton
OrderDto dto = OrderMapper.Instance.Map(order);

// Option B: DI registration
services.AddSingleton<OrderMapper>();
// ...
OrderDto dto = mapper.Map(order);
```

---

## Reverse Mapping

Declare both directions and the generator implements both:

```csharp
[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
    public partial Order    Map(OrderDto source);   // reverse
}
```

---

## Nested Type Mapping

Declare a method for each nested type — the generator chains them automatically:

```csharp
[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto    Map(Order source);
    public partial AddressDto  Map(Address source);  // auto-used for Order.Address
}
```

The generated `Map(Order)` emits `Address = Map(source.Address)`.

---

## Collection Mapping

If a destination property is `List<TDst>` and source is a list-like type, and you have a partial method that maps `TSrc → TDst`:

```csharp
[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);       // Order.Lines → OrderDto.Lines
    public partial LineDto  Map(Line source);        // element mapper
}
// Generated: Lines = source.Lines?.Select(Map).ToList() ?? new List<LineDto>()
```

---

## Custom Converter Methods

Add non-partial methods to the mapper class — the generator detects them by signature and uses them automatically:

```csharp
[EggMapper]
public partial class PersonMapper
{
    public partial PersonDto Map(Person source);

    // Converter: DateTime → string — used automatically for Birthday property
    private string FormatDate(DateTime d) => d.ToString("yyyy-MM-dd");
}
```

---

## Enum Mapping

Enum-to-enum properties are mapped with an explicit cast.  If underlying types differ, EGG3003 (Info) is reported.

```csharp
[EggMapper]
public partial class StatusMapper
{
    public partial StatusDto Map(Status source);
}
// Generated: Kind = (StatusDtoKind)source.Kind,
```

---

## Diagnostics

| Code | Severity | Description |
|------|----------|-------------|
| EGG3001 | Warning | A writable destination property has no matching source — left at default |
| EGG3002 | Warning | `[EggMapper]` class has no partial mapping methods |
| EGG3003 | Info | Enum-to-enum cast where underlying types differ |

---

## When to Use Class Mapper vs Attribute Mapper

| Feature | Attribute Mapper `[MapTo]` | Class Mapper `[EggMapper]` |
|---------|-----------------|---------------------|
| Simple property copy | ✅ | ✅ |
| Reverse mapping | ❌ | ✅ |
| Multiple destinations from one source | ✅ | ✅ |
| Custom converter logic alongside generated | Limited (AfterMap hook) | ✅ full methods |
| DI-friendly mapper instance | ❌ (static extension) | ✅ (Instance + ctor) |
| Property rename/ignore | ✅ `[MapProperty]`/`[MapIgnore]` | via converter method |

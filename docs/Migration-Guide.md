---
layout: default
title: Migration Guide
nav_order: 8
description: "Migrate from AutoMapper to EggMapper, or from runtime to compile-time mapping tiers."
---

# Migration Guide
{: .no_toc }

## Table of contents
{: .no_toc .text-delta }

1. TOC
{:toc}

---

## Migrating from AutoMapper to EggMapper

This is the most common migration path. EggMapper is a drop-in replacement for AutoMapper with the same API.

### Step-by-step

**1. Swap NuGet packages:**

```bash
dotnet remove package AutoMapper
dotnet remove package AutoMapper.Extensions.Microsoft.DependencyInjection
dotnet add package EggMapper
```

**2. Find and replace the namespace:**

```diff
- using AutoMapper;
+ using EggMapper;
```

**3. Update DI registration:**

```diff
- services.AddAutoMapper(typeof(MyProfile).Assembly);
+ services.AddEggMapper(typeof(MyProfile).Assembly);
```

**4. Update ProjectTo calls (if any):**

```diff
- query.ProjectTo<ProductDto>(config);
+ query.ProjectTo<Product, ProductDto>(config);
```

**5. Run tests.** All your existing `CreateMap`, `ForMember`, `Profile`, and `IMapper` code works unchanged.

### What stays the same

These APIs are identical between AutoMapper and EggMapper:

- `CreateMap<TSrc, TDst>()`
- `ForMember(d => d.Prop, o => o.MapFrom(...))`
- `ForMember(d => d.Prop, o => o.Ignore())`
- `ForMember(d => d.Prop, o => o.Condition(...))`
- `ForPath(d => d.A.B.Prop, o => o.MapFrom(...))`
- `ReverseMap()`
- `BeforeMap()` / `AfterMap()`
- `MaxDepth()`
- `IncludeBase<TBaseSrc, TBaseDst>()`
- `Profile` base class
- `IMapper` interface
- `mapper.Map<TSrc, TDst>(source)`
- `mapper.Map<TDst>(objectSource)`
- `config.AssertConfigurationIsValid()`

### What is different

| AutoMapper | EggMapper | Notes |
|------------|-----------|-------|
| `ProjectTo<TDest>(config)` | `ProjectTo<TSrc, TDest>(config)` | Explicit source type parameter |
| `AddAutoMapper(assemblies)` | `AddEggMapper(assemblies)` | Same behavior, different name |
| `IMapper` registered as scoped | `IMapper` registered as singleton | Safe because config is immutable |
| `CreateMap<T,T>()` required for clone | Auto-compiles on first use | Just call `Map<T,T>(obj)` |
| No built-in Patch | `mapper.Patch(src, dst)` | New feature |
| No built-in MapList | `mapper.MapList<S,D>(list)` | New feature |
| No inline validation | `.Validate(d => d.Prop, pred, msg)` | New feature |

### Migration example: full controller

**Before (AutoMapper):**

```csharp
using AutoMapper;

public class OrdersController(IMapper mapper, AppDbContext db) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? NotFound() : Ok(mapper.Map<OrderDto>(order));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await db.Orders.ToListAsync();
        return Ok(mapper.Map<List<OrderSummaryDto>>(orders));
    }
}
```

**After (EggMapper):**

```csharp
using EggMapper;

public class OrdersController(IMapper mapper, AppDbContext db) : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await db.Orders.Include(o => o.Customer).FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? NotFound() : Ok(mapper.Map<Order, OrderDto>(order));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await db.Orders.ToListAsync();
        return Ok(mapper.MapList<Order, OrderSummaryDto>(orders));  // faster batch mapping
    }
}
```

The only changes are the `using` directive and optionally switching to the generic `Map<TSrc, TDst>()` and `MapList<>()` for better performance.
{: .note }

### You can also remove unnecessary `CreateMap<T, T>()` calls

If you had self-referencing maps for cloning, EggMapper handles these automatically:

```diff
  // Before
  cfg.CreateMap<Product, Product>();

  // After — remove the line, it auto-compiles
```

---

## Runtime to Compile-Time Mapping

EggMapper supports **three tiers** of mapping. This guide walks through migrating from the runtime API (Tier 1 style) to the compile-time generators (Tier 2 and Tier 3).

### Tier overview

| Tier | Package | API | When to use |
|------|---------|-----|-------------|
| Runtime | `EggMapper` | `MapperConfiguration` + `CreateMap` | Complex/conditional mapping, existing AutoMapper code |
| Tier 2 | `EggMapper.Generator` | `[MapTo]` attribute | Simple 1:1 copies, want compile-time safety |
| Tier 3 | `EggMapper.ClassMapper` | `[EggMapper]` partial class | Custom logic alongside generated code, DI, reverse mapping |

You can mix all three tiers in the same project — there is no requirement to migrate everything at once.

---

### Migrating a simple `CreateMap` to `[MapTo]`

#### Before (runtime)

```csharp
var config = new MapperConfiguration(cfg =>
{
    cfg.CreateMap<Order, OrderDto>();
});
var mapper = config.CreateMapper();

// Usage
var dto = mapper.Map<OrderDto>(order);
```

#### After (Tier 2 -- compile-time extension method)

```csharp
// 1. Add [MapTo] to the source class
[MapTo(typeof(OrderDto))]
public class Order
{
    public int    Id    { get; set; }
    public string Name  { get; set; } = "";
}

// 2. Use the generated extension
var dto = order.ToOrderDto();
```

**What you gain:**
- Mapping errors reported at **build time** (not runtime)
- Zero startup cost — no `MapperConfiguration` needed
- Zero reflection at call time

---

### Migrating a `CreateMap` with `ForMember` to `[EggMapper]`

#### Before (runtime with customization)

```csharp
cfg.CreateMap<Customer, CustomerDto>()
   .ForMember(d => d.DisplayLabel,
              o => o.MapFrom(s => $"{s.FirstName} {s.LastName}"));
```

#### After (Tier 3 -- partial class mapper)

```csharp
[EggMapper]
public partial class CustomerMapper
{
    public partial CustomerDto Map(Customer source);

    // Custom logic lives alongside the generated code
    private string BuildDisplayLabel(Customer s)
        => $"{s.FirstName} {s.LastName}";
}
```

The generator maps all matching properties automatically and uses `BuildDisplayLabel(Customer)` automatically if `CustomerDto.DisplayLabel` is of type `string`.

---

### Migrating `ReverseMap`

#### Before (runtime)

```csharp
cfg.CreateMap<Order, OrderDto>().ReverseMap();
```

#### After (Tier 3)

```csharp
[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
    public partial Order    Map(OrderDto source);
}
```

---

### Migrating `Profile` classes

If you have a large `Profile` subclass, the Analyzer EGG1003 will flag each bare `CreateMap<S,D>()` call that has no customizations and suggest replacing it with `[MapTo]`. Use the IDE quick-fix to apply the suggestion automatically.

```
// EGG1003 (Info): CreateMap<Order, OrderDto>() has no customizations.
// Consider using [MapTo(typeof(OrderDto))] on Order with EggMapper.Generator
// for compile-time type safety and zero-overhead mapping.
```

---

## Keeping the Runtime API

Not everything needs to migrate. Keep `CreateMap` + `ForMember` for:

- Maps with `Condition`, `PreCondition`, `AfterMap` hooks
- `MaxDepth` / `IncludeBase` / polymorphic inheritance
- Dynamic map selection at runtime
- Patch mapping
- Inline validation
- Open generic maps

The runtime API is fully retained and will not be removed.

---

## Side-by-side checklist

| Runtime feature | Tier 2 equivalent | Tier 3 equivalent |
|----------------|------------------|------------------|
| `CreateMap<S,D>()` | `[MapTo(typeof(D))]` on S | partial method in `[EggMapper]` class |
| `ForMember` rename | `[MapProperty("Name")]` on source property | Custom converter method |
| `Ignore()` | `[MapIgnore]` on source property | Omit from destination |
| `ReverseMap()` | Add `[MapTo(typeof(S))]` on D | Add reverse partial method |
| `AfterMap(...)` hook | `static partial void AfterMap(S,D)` | Custom method in mapper class |
| Collection mapping | `ToOrderDtoList()` | Automatic via `List<TDst>` detection |
| Nested type mapping | Declare `[MapTo]` on nested type too | Declare additional partial method |
| DI singleton | N/A (static extension) | `MyMapper.Instance` + constructor injection |

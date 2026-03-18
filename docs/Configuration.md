# Configuration

## `MapperConfiguration`

`MapperConfiguration` is the **entry point** for EggMapper. You construct it once (typically at startup) and keep a single instance for the lifetime of the application.

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg =>
{
    // Register maps here
    cfg.CreateMap<Source, Destination>();
});
```

> ⚠️ `MapperConfiguration` is **not** thread-safe to modify after construction, but it is safe to read (call `CreateMapper`) concurrently.

---

## Registering Maps

### Basic map

```csharp
cfg.CreateMap<Source, Destination>();
```

By convention, properties with identical names (case-insensitive) are mapped automatically.

### Reverse map

```csharp
cfg.CreateMap<Source, Destination>().ReverseMap();
// Registers both Source→Destination and Destination→Source
```

### Multiple maps

```csharp
cfg.CreateMap<Customer, CustomerDto>();
cfg.CreateMap<Address,  AddressDto>();
cfg.CreateMap<Order,    OrderDto>();
```

---

## Adding Profiles

Group related maps in a [`Profile`](Profiles) class and register the entire profile at once:

```csharp
cfg.AddProfile<OrderProfile>();
cfg.AddProfile<CustomerProfile>();
```

Or scan an assembly for all profiles:

```csharp
cfg.AddProfiles(typeof(OrderProfile).Assembly);
```

---

## Creating a Mapper

```csharp
IMapper mapper = config.CreateMapper();
```

`CreateMapper()` returns a lightweight `IMapper` instance backed by the compiled delegate cache. You may call it multiple times; each call returns a new wrapper around the same shared cache.

---

## Configuration Validation

Validate that every destination property has a source mapping (catches typos and missing maps early):

```csharp
config.AssertConfigurationIsValid();
```

Call this in unit tests or at application startup to surface misconfiguration immediately.

```csharp
// Throws InvalidOperationException if any destination property is unmapped
config.AssertConfigurationIsValid();
```

---

## `IMapperConfigurationExpression` Options

| Method | Description |
|--------|-------------|
| `CreateMap<TSrc, TDst>()` | Register a new type map and return the fluent builder |
| `AddProfile<TProfile>()` | Register all maps declared in a `Profile` subclass |
| `AddProfile(profile)` | Register a pre-constructed `Profile` instance |
| `AddProfiles(assemblies)` | Scan assemblies and register all `Profile` subclasses |

---

## Thread Safety

| Scenario | Safe? |
|----------|-------|
| Construct `MapperConfiguration` on one thread | ✅ |
| Call `CreateMapper()` concurrently | ✅ |
| Call `IMapper.Map()` concurrently | ✅ |
| Add maps after construction | ❌ Not supported |

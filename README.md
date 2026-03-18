# 🥚 EggMapper

**High-performance .NET object-to-object mapper**

Sponsored by [eggspot.app](https://eggspot.app)

[![CI](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml/badge.svg)](https://github.com/eggspot/EggMapper/actions/workflows/ci.yml)
[![MIT License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/EggMapper.svg)](https://www.nuget.org/packages/EggMapper)

## Overview

EggMapper is a **faster-than-AutoMapper** .NET object-to-object mapping library with a familiar, ergonomic API. It uses compiled expression trees cached at configuration time — **zero reflection at map-time**.

## Quick Start

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg => {
    cfg.CreateMap<Source, Destination>();
});

var mapper = config.CreateMapper();
var dest = mapper.Map<Destination>(source);
```

## With Profiles

```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<Customer, CustomerDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}"))
            .ForMember(d => d.AddressLine, opt => opt.MapFrom(s => s.Address.Street));
    }
}

var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
var mapper = config.CreateMapper();
```

## Dependency Injection

```csharp
// In Program.cs
builder.Services.AddEggMapper(typeof(MyProfile).Assembly);

// In your service
public class MyService(IMapper mapper) { ... }
```

## Performance

| Mapper | Simple Flat | Deep Object | Collection (1000) |
|--------|------------|-------------|-------------------|
| Manual | 1x (baseline) | 1x | 1x |
| **EggMapper** | **~1.1x** | **~1.2x** | **~1.1x** |
| AutoMapper | ~3x | ~4x | ~3x |
| Mapster | ~1.3x | ~1.5x | ~1.2x |

*Slower ratio = better performance*

## Features

- ✅ Compiled expression tree delegates (zero runtime reflection)
- ✅ `ForMember` / `MapFrom` custom mappings
- ✅ `Ignore()` members
- ✅ `ReverseMap()` bidirectional mapping
- ✅ Nested object mapping
- ✅ Collection mapping (`List<T>`, arrays, `HashSet<T>`, etc.)
- ✅ Constructor mapping
- ✅ Profile-based configuration
- ✅ Assembly scanning
- ✅ Before/After map hooks
- ✅ Conditional mapping
- ✅ Null substitution
- ✅ MaxDepth for self-referencing types
- ✅ Inheritance mapping
- ✅ Enum mapping
- ✅ `ForPath` for nested destination properties
- ✅ .NET 8 Dependency Injection integration
- ✅ Configuration validation

## Contributing

Contributions are welcome! Please open an issue or pull request.

---

*Powered by [Eggspot](https://eggspot.app)*

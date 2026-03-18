# 🥚 EggMapper — Wiki Home

Welcome to the **EggMapper** documentation wiki.

EggMapper is a **high-performance .NET object-to-object mapping library** built on compiled expression trees. It provides a familiar API inspired by AutoMapper while being significantly faster at runtime — with **zero reflection at map-time**.

---

## 📚 Table of Contents

| Page | What you'll learn |
|------|-------------------|
| [Getting Started](Getting-Started) | Install the package, create your first mapping |
| [Configuration](Configuration) | `MapperConfiguration` options, `CreateMap`, validation |
| [Profiles](Profiles) | Organise maps in reusable `Profile` classes |
| [Dependency Injection](Dependency-Injection) | ASP.NET Core / `Microsoft.Extensions.DependencyInjection` setup |
| [Advanced Features](Advanced-Features) | `ForMember`, `Ignore`, `ReverseMap`, conditions, hooks, and more |
| [Performance](Performance) | Benchmark results, methodology, and optimisation tips |
| [API Reference](API-Reference) | Full public API surface |

---

## ⚡ 30-Second Quickstart

```bash
dotnet add package EggMapper
```

```csharp
using EggMapper;

var config = new MapperConfiguration(cfg =>
    cfg.CreateMap<Order, OrderDto>());

IMapper mapper = config.CreateMapper();

OrderDto dto = mapper.Map<Order, OrderDto>(order);
```

---

## 🏗️ Design Principles

1. **Zero runtime reflection** — all delegates compiled once at `MapperConfiguration` construction time and cached in a `ConcurrentDictionary`.
2. **Familiar API** — drop-in mental model for anyone who has used AutoMapper.
3. **Small surface area** — only the features you actually need, no magic.

---

*Source repository: [github.com/eggspot/EggMapper](https://github.com/eggspot/EggMapper)*  
*NuGet: [EggMapper](https://www.nuget.org/packages/EggMapper) · [EggMapper.DependencyInjection](https://www.nuget.org/packages/EggMapper.DependencyInjection)*

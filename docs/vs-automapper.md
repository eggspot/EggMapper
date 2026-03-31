---
layout: default
title: EggMapper vs AutoMapper
nav_order: 3
description: "Compare EggMapper and AutoMapper for .NET object mapping. Same API, 2-5x faster, MIT licensed."
---

# EggMapper vs AutoMapper

## Why switch from AutoMapper?

AutoMapper changed to a commercial license (RPL) starting v13. EggMapper is a **free, MIT-licensed** drop-in replacement that's also **2-5x faster**.

## Comparison

| Feature | AutoMapper 16.x | EggMapper |
|---------|----------------|-----------|
| **License** | RPL 1.5 (commercial) | MIT (free forever) |
| **Performance** | Baseline | **2-5x faster** |
| **Allocations** | Extra per-map | **Zero extra** |
| **Runtime reflection** | Yes | **No** (compiled expressions) |
| **API** | Original | **Same API** — drop-in |
| **CreateMap / ForMember** | Yes | Yes (identical) |
| **Profile** | Yes | Yes (identical) |
| **IMapper** | Yes | Yes (identical) |
| **DI registration** | `AddAutoMapper()` | `AddEggMapper()` |
| **EF Core ProjectTo** | `ProjectTo<D>(cfg)` | `ProjectTo<S,D>(cfg)` |
| **Null collections** | Empty by default | Empty by default |
| **EF Core proxies** | Supported | Supported |
| **Same-type T→T** | Needs CreateMap | **Auto-compiles** |
| **Collection auto-map** | Automatic | Automatic |
| **Patch/partial** | Not built-in | **Built-in** |

## Migration (5 minutes)

```diff
- dotnet add package AutoMapper
+ dotnet add package EggMapper

- using AutoMapper;
+ using EggMapper;

- services.AddAutoMapper(typeof(MyProfile).Assembly);
+ services.AddEggMapper(typeof(MyProfile).Assembly);
```

All your existing `CreateMap`, `ForMember`, `Profile`, and `IMapper` code works unchanged.

## Benchmark Results

EggMapper consistently outperforms AutoMapper on every scenario:

- **Flat mapping**: 2-3x faster
- **Nested objects**: 2-4x faster
- **Collections**: 3-5x faster
- **Deep object graphs**: 2-4x faster

Zero extra allocations in all scenarios — matches hand-written code.

[View full benchmark results on GitHub](https://github.com/eggspot/EggMapper#benchmarks)

## Get Started

```bash
dotnet add package EggMapper
```

[Quick Start Guide](quick-start) | [API Reference](API-Reference) | [GitHub](https://github.com/eggspot/EggMapper)

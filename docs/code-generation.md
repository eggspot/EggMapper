---
layout: default
title: Code Generation
nav_order: 6
has_children: true
description: "EggMapper compile-time code generation — attribute mapper and class mapper for zero-overhead mapping."
---

# Code Generation

EggMapper supports compile-time source generators for zero-overhead mapping alongside the runtime API.

| Approach | Package | When to use |
|----------|---------|-------------|
| **Attribute Mapper** | `EggMapper.Generator` | Simple 1:1 copies, want compile-time safety |
| **Class Mapper** | `EggMapper.ClassMapper` | Custom logic + generated code, DI, reverse mapping |

You can mix runtime and generated mappings in the same project.

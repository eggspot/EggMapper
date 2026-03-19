---
name: Bug Report
about: Report a bug in EggMapper
title: ''
labels: bug
assignees: ''
---

**Describe the bug**
A clear description of what the bug is.

**To Reproduce**
```csharp
// Minimal code to reproduce the issue
var config = new MapperConfiguration(cfg => {
    cfg.CreateMap<Source, Dest>();
});
var mapper = config.CreateMapper();
var result = mapper.Map<Dest>(source);
```

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened (include exception message/stack trace if applicable).

**Environment**
- EggMapper version:
- .NET version:
- OS:

---
name: expr-debug
description: Inspect and debug EggMapper's compiled expression trees for a type pair
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Grep, Glob
---

# Expression Tree Debugger

Inspect what expression tree EggMapper compiles for a specific source→destination type pair. Helps diagnose why a property is unmapped, null, or taking the slow path.

## Arguments

- `$ARGUMENTS` — the type pair or symptom to debug (e.g., "OrderDto to Order", "why is Address null", "which path does UserDto use")

## Steps

1. **Find the relevant types** in the codebase:
   - Search `src/EggMapper.UnitTests/` and `src/EggMapper.Benchmarks/` for the types mentioned
   - Identify the source and destination types

2. **Trace the compilation path** in `src/EggMapper/Execution/ExpressionBuilder.cs`:
   - Check if the type pair would qualify for **ctx-free path** (`TryBuildCtxFreeDelegate`) — no hooks, no conditions, no MaxDepth, has parameterless ctor
   - Check if it would use **typed delegate path** (`TryBuildTypedDelegate`) — same constraints but boxed
   - Or if it falls to **flexible path** (`BuildFlexibleDelegate`) — per-property action arrays

3. **For each destination property**, trace which assignment method handles it:
   - Direct same-type assignment?
   - Numeric conversion?
   - Nullable unwrap/wrap?
   - Inlined nested object (`TryBuildCtxFreeNestedAssign`)?
   - Inlined collection (`TryBuildCtxFreeCollectionAssign`)?
   - Flattened property (`TryBuildTypedFlattenedAssign`)?
   - Or falls through unmapped?

4. **Report findings**:

   ### Expression Debug: {SourceType} → {DestType}

   **Delegate path**: ctx-free / typed / flexible (and WHY)

   **Property mapping**:
   | Dest Property | Source | Method | Notes |
   |---------------|--------|--------|-------|

   **Potential issues**:
   - Properties that fall through unmapped
   - Properties forcing the map to flexible path
   - Type mismatches that could be auto-converted

5. **If a property is unexpectedly null/unmapped**, explain exactly why and suggest the fix (ForMember, type conversion, missing CreateMap for nested type, etc.)

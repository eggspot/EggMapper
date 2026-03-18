# EggMapper — AI Coding Instructions

## Project Purpose
EggMapper is a high-performance .NET object-to-object mapping library. The goal is to be **faster than any other mapper library** (AutoMapper, Mapster, etc.) while maintaining a familiar API. All code changes must follow the two development loops below.

---

## 🔁 Loop 1 — Correctness: Implement → Test → Fix → Repeat

**Always follow this cycle for every feature or bug fix:**

1. **Write or update the feature code** in `src/EggMapper/`
2. **Write or update unit tests** in `src/EggMapper.UnitTests/`
   - Use **xUnit** + **FluentAssertions**
   - Every public API method needs at least one test
   - Cover happy path, null inputs, edge cases, and error conditions
3. **Run tests** — do not move on until all tests are green:
   ```bash
   dotnet build --configuration Release
   dotnet test src/EggMapper.UnitTests/EggMapper.UnitTests.csproj --configuration Release --no-build
   ```
4. **If tests fail → fix the code → re-run tests → repeat until 100% green**

### Test naming convention
```
MethodOrFeature_StateUnderTest_ExpectedBehavior
```
Example: `Map_NullSource_ReturnsDefault`, `ForMember_WithIgnore_LeavesPropertyDefault`

### Test structure (AAA pattern)
```csharp
[Fact]
public void Feature_Condition_Expected()
{
    // Arrange
    var config = new MapperConfiguration(cfg => cfg.CreateMap<Src, Dst>());
    var mapper = config.CreateMapper();

    // Act
    var result = mapper.Map<Src, Dst>(source);

    // Assert
    result.Property.Should().Be(expected);
}
```

---

## 🔁 Loop 2 — Performance: Benchmark → Measure → Optimize → Repeat

**Always follow this cycle for any performance work:**

1. **Run the baseline benchmark** before making changes — record current numbers:
   ```bash
   cd src/EggMapper.Benchmarks
   dotnet run --configuration Release -- --filter *FlatMapping* --join
   ```
2. **Identify which benchmark is slower than any competing mapper**
3. **Optimize the relevant code** in `src/EggMapper/Execution/ExpressionBuilder.cs` or `src/EggMapper/Mapper.cs`
4. **Re-run the benchmark** — compare against the recorded baseline
5. **If still slower → profile → fix → re-benchmark → repeat until EggMapper beats all mappers**

### Running all benchmarks
```bash
cd src/EggMapper.Benchmarks
dotnet run --configuration Release -- --filter * --exporters json markdown
```

### Running a single benchmark class
```bash
dotnet run --configuration Release -- --filter *FlatMappingBenchmark*
```

### Performance targets
| Scenario | Target |
|---|---|
| Flat mapping | Fastest among AutoMapper, Mapster, and Manual |
| Deep/nested mapping | Fastest among AutoMapper, Mapster, and Manual |
| Collection (100 items) | Fastest among AutoMapper, Mapster, and Manual |
| Startup / config | At least as fast as AutoMapper and Mapster |

> **NEVER accept a benchmark result where EggMapper is not the fastest mapper.**
> If a benchmark regresses, revert or fix before merging.

---

## 🏗️ Architecture Constraints

- **Zero reflection at map time** — all delegates compiled in `MapperConfiguration` constructor
- **Compiled delegates cached** in `ConcurrentDictionary<TypePair, Func<object, object?, ResolutionContext, object>>`
- **No LINQ in hot paths** — use `for` loops with pre-sized collections
- **AggressiveInlining** on `IMapper.Map<S,D>()` and delegate lookup
- **Value-type `TypePair` key** for dictionary lookups (no boxing)
- **Pre-compiled child mappers** embedded in parent delegates (no per-call lookup for nested types)

---

## 📁 Project Structure

```
src/EggMapper/                   ← Core library (compiled delegates, zero reflection at runtime)
src/EggMapper.DependencyInjection/ ← .NET 8 DI integration
src/EggMapper.UnitTests/         ← xUnit + FluentAssertions (must stay 100% green)
src/EggMapper.Benchmarks/        ← BenchmarkDotNet (EggMapper vs AutoMapper vs Mapster vs Manual)
```

---

## 🔖 Working Style

- **Break complex or long-running tasks into smaller incremental commits.** Commit and push after each verified, self-contained unit of work rather than in one large batch. This avoids timeouts and makes the PR easier to review.
- A single PR should focus on one logical change. If a task naturally splits into independent pieces (e.g. feature + docs + benchmarks), deliver them in separate commits with clear messages.

---

## ✅ Definition of Done

- All unit tests pass: `dotnet test --configuration Release` exits 0
- EggMapper is faster than all competing mappers (AutoMapper, Mapster) on every benchmark scenario
- No runtime reflection (`PropertyInfo.GetValue/SetValue`) in hot paths
- New features have corresponding tests before merging

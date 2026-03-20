---
name: test
description: Run EggMapper tests — all tests or filtered by class/method name
disable-model-invocation: true
user-invocable: true
allowed-tools: Bash, Read, Grep
---

# Test Runner

Run EggMapper unit tests with optional filtering.

## Arguments

- `$ARGUMENTS` — optional test filter. Examples:
  - Empty → run all tests
  - `Flattening` → run tests matching "Flattening"
  - `EnumConversion` → run tests matching "EnumConversion"
  - `Map_NullSource` → run a specific test method

## Steps

1. **Build and run tests**:
   - If `$ARGUMENTS` is empty:
     ```bash
     dotnet test src/EggMapper.UnitTests/EggMapper.UnitTests.csproj --configuration Release
     ```
   - If `$ARGUMENTS` is provided:
     ```bash
     dotnet test src/EggMapper.UnitTests/EggMapper.UnitTests.csproj --configuration Release --filter "FullyQualifiedName~$ARGUMENTS"
     ```

2. **On failure**:
   - Read the failing test to understand the assertion
   - Read the relevant source code
   - Explain what's wrong and suggest a fix
   - Do NOT auto-fix unless the user asks

3. **On success**: Report the pass count concisely. No verbose output needed.

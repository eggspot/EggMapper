; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
EGG1002 | EggMapper | Warning | MapperConfiguration constructed without AssertConfigurationIsValid
EGG1003 | EggMapper | Info | Simple CreateMap with no customizations — suggest compile-time [MapTo]

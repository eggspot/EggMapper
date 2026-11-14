using Microsoft.CodeAnalysis;

namespace EggMapper.Analyzers
{
    internal static class EggMapperDiagnostics
    {
        private const string Category = "EggMapper";
        private const string HelpLinkBase = "https://eggspot.github.io/EggMapper/diagnostics/";

        public static readonly DiagnosticDescriptor MissingAssertConfigurationIsValid = new DiagnosticDescriptor(
            id: "EGG1002",
            title: "MapperConfiguration missing AssertConfigurationIsValid",
            messageFormat: "MapperConfiguration is constructed but AssertConfigurationIsValid() is never called. " +
                           "Call config.CompileMappings() and config.AssertConfigurationIsValid() in a test or at startup " +
                           "to catch unmapped members before they fail at run time.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Calling AssertConfigurationIsValid() validates that every destination property is " +
                         "mapped and throws a descriptive exception at startup rather than silently failing at runtime.",
            helpLinkUri: HelpLinkBase + "EGG1002.html",
            customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

        public static readonly DiagnosticDescriptor SuggestCompileTimeMapping = new DiagnosticDescriptor(
            id: "EGG1003",
            title: "Consider compile-time mapping with [MapTo]",
            messageFormat: "CreateMap<{0}, {1}>() has no customizations. " +
                           "Consider using [MapTo(typeof({1}))] on {0} with EggMapper.Generator " +
                           "for mapping code generated and checked at build time.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "EggMapper.Generator generates extension methods at build time, without reflection, " +
                         "from simple [MapTo] attribute declarations, catching mapping errors at build time.",
            helpLinkUri: HelpLinkBase + "EGG1003.html");
    }
}

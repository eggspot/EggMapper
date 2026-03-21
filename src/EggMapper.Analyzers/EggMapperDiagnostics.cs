using Microsoft.CodeAnalysis;

namespace EggMapper.Analyzers
{
    internal static class EggMapperDiagnostics
    {
        private const string Category = "EggMapper";
        private const string HelpLinkBase = "https://github.com/eggspot/EggMapper/blob/main/docs/diagnostics/";

        /// <summary>
        /// EGG1002: MapperConfiguration constructed without a subsequent AssertConfigurationIsValid() call.
        /// </summary>
        public static readonly DiagnosticDescriptor MissingAssertConfigurationIsValid = new DiagnosticDescriptor(
            id: "EGG1002",
            title: "MapperConfiguration missing AssertConfigurationIsValid",
            messageFormat: "MapperConfiguration is constructed but AssertConfigurationIsValid() is never called. " +
                           "Add config.AssertConfigurationIsValid() (or mapper.ConfigurationProvider.AssertConfigurationIsValid()) " +
                           "at startup to catch missing maps before they fail at runtime.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Calling AssertConfigurationIsValid() validates that every destination property is " +
                         "mapped and throws a descriptive exception at startup rather than silently failing at runtime.",
            helpLinkUri: HelpLinkBase + "EGG1002.md",
            customTags: new[] { WellKnownDiagnosticTags.CompilationEnd });

        /// <summary>
        /// EGG1003: Simple CreateMap with no customizations — suggest compile-time [MapTo].
        /// </summary>
        public static readonly DiagnosticDescriptor SuggestCompileTimeMapping = new DiagnosticDescriptor(
            id: "EGG1003",
            title: "Consider compile-time mapping with [MapTo]",
            messageFormat: "CreateMap<{0}, {1}>() has no customizations. " +
                           "Consider using [MapTo(typeof({1}))] on {0} with EggMapper.Generator " +
                           "for compile-time type safety and zero-overhead mapping.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: "EggMapper.Generator can generate fully compile-time, zero-reflection extension methods " +
                         "from simple [MapTo] attribute declarations, catching mapping errors at build time.",
            helpLinkUri: HelpLinkBase + "EGG1003.md");
    }
}

using Microsoft.CodeAnalysis;

namespace EggMapper.ClassMapper
{
    internal static class Diagnostics
    {
        private const string Category = "EggMapper";
        private const string HelpBase = "https://github.com/eggspot/EggMapper/blob/main/docs/diagnostics/";

        /// <summary>
        /// EGG3001: A writable destination property has no matching source member.
        /// </summary>
        public static readonly DiagnosticDescriptor UnmappedDestinationProperty = new DiagnosticDescriptor(
            id: "EGG3001",
            title: "Unmapped destination property",
            messageFormat: "Property '{0}' on '{1}' has no matching source in '{2}' and will be left at its default value",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Add a matching source property, a custom converter method, or annotate with [MapIgnore] to silence this warning.",
            helpLinkUri: HelpBase + "EGG3001.md");

        /// <summary>
        /// EGG3002: [EggMapper] class declares no partial mapping methods.
        /// </summary>
        public static readonly DiagnosticDescriptor NoMappingMethodsDeclared = new DiagnosticDescriptor(
            id: "EGG3002",
            title: "No mapping methods declared",
            messageFormat: "[EggMapper] class '{0}' has no partial mapping methods (methods with a single parameter and non-void return type)",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Declare one or more partial methods, for example: public partial OrderDto Map(Order source).",
            helpLinkUri: HelpBase + "EGG3002.md");

        /// <summary>
        /// EGG3003: Enum-to-enum mapping where underlying types differ.
        /// </summary>
        public static readonly DiagnosticDescriptor EnumUnderlyingTypeMismatch = new DiagnosticDescriptor(
            id: "EGG3003",
            title: "Enum underlying type mismatch",
            messageFormat: "Mapping enum '{0}' to '{1}': underlying types differ. An explicit cast is generated but values may not correspond.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpBase + "EGG3003.md");
    }
}

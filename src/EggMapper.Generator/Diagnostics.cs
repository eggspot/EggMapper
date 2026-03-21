using Microsoft.CodeAnalysis;

namespace EggMapper.Generator
{
    internal static class Diagnostics
    {
        private const string Category = "EggMapper";

        /// <summary>EGG2001: A destination property has no matching source property and is not ignored.</summary>
        public static readonly DiagnosticDescriptor UnmappedDestinationProperty = new DiagnosticDescriptor(
            id: "EGG2001",
            title: "Unmapped destination property",
            messageFormat: "Destination property '{0}.{1}' has no matching source property on '{2}'. " +
                           "Add [MapProperty] to redirect, [MapIgnore] to suppress, or add a matching property.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Every writable destination property must have a corresponding source property, " +
                         "a [MapProperty] redirect, or must be silenced with [MapIgnore] on the source side.");

        /// <summary>EGG2002: Source and destination property types are not directly assignable — a cast is emitted.</summary>
        public static readonly DiagnosticDescriptor TypeMismatch = new DiagnosticDescriptor(
            id: "EGG2002",
            title: "Implicit type conversion in mapping",
            messageFormat: "Source property '{0}.{1}' ({2}) is not directly assignable to destination property '{3}.{4}' ({5}). " +
                           "An explicit cast will be emitted; verify this is intentional.",
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "The source and destination property types differ. EggMapper.Generator emits an explicit " +
                         "cast, which may fail at runtime if the types are incompatible.");
    }
}

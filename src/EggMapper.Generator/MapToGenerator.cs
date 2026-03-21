using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace EggMapper.Generator
{
    [Generator]
    public sealed class MapToGenerator : IIncrementalGenerator
    {
        private const string MapToFqn       = "EggMapper.MapToAttribute";
        private const string MapPropertyFqn = "EggMapper.MapPropertyAttribute";
        private const string MapIgnoreFqn   = "EggMapper.MapIgnoreAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Inject attribute definitions into every compilation that references this generator
            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("EggMapper.MapToAttribute.g.cs",
                    SourceText.From(AttributeSource.MapToAttribute, Encoding.UTF8));
                ctx.AddSource("EggMapper.MapPropertyAttribute.g.cs",
                    SourceText.From(AttributeSource.MapPropertyAttribute, Encoding.UTF8));
                ctx.AddSource("EggMapper.MapIgnoreAttribute.g.cs",
                    SourceText.From(AttributeSource.MapIgnoreAttribute, Encoding.UTF8));
            });

            // Find every type decorated with [MapTo(...)]
            IncrementalValuesProvider<MappingRequest> mappingRequests = context
                .SyntaxProvider
                .ForAttributeWithMetadataName(
                    MapToFqn,
                    predicate: static (node, _) => node is TypeDeclarationSyntax,
                    transform: static (ctx, ct) => TransformMappingRequest(ctx, ct))
                .Where(static r => r is not null)!;

            context.RegisterSourceOutput(mappingRequests, static (spc, request) =>
            {

                foreach (var diag in request.Diagnostics)
                    spc.ReportDiagnostic(diag);

                if (request.GeneratedSource is not null)
                    spc.AddSource(request.HintName, SourceText.From(request.GeneratedSource, Encoding.UTF8));
            });
        }

        // ── Transform ────────────────────────────────────────────────────────

        private static MappingRequest? TransformMappingRequest(
            GeneratorAttributeSyntaxContext ctx,
            System.Threading.CancellationToken ct)
        {
            if (ctx.TargetSymbol is not INamedTypeSymbol sourceType)
                return null;

            var diagnostics = new List<Diagnostic>();
            var allSources  = new StringBuilder();
            var hintParts   = new List<string>();

            foreach (var attrData in ctx.Attributes)
            {
                ct.ThrowIfCancellationRequested();

                if (attrData.ConstructorArguments.Length != 1)
                    continue;

                var destTypeArg = attrData.ConstructorArguments[0];
                if (destTypeArg.Value is not INamedTypeSymbol destType)
                    continue;

                var (src, diags) = GenerateMapping(sourceType, destType, ctx.SemanticModel.Compilation);
                diagnostics.AddRange(diags);

                if (src is not null)
                {
                    allSources.Append(src);
                    hintParts.Add($"{sourceType.Name}To{destType.Name}Extensions");
                }
            }

            // Even with no generated source we must return a request to emit any diagnostics
            string hint = hintParts.Count > 0
                ? $"{string.Join("_", hintParts)}.g.cs"
                : $"{sourceType.Name}_EggMapper_errors.g.cs";

            string? finalSource = allSources.Length > 0 ? allSources.ToString() : null;
            return new MappingRequest(hint, finalSource, diagnostics);
        }

        // ── Code generation ──────────────────────────────────────────────────

        private static (string? source, List<Diagnostic> diagnostics) GenerateMapping(
            INamedTypeSymbol sourceType,
            INamedTypeSymbol destType,
            Compilation compilation)
        {
            var diagnostics = new List<Diagnostic>();

            // Collect source properties (readable, instance)
            var srcProps = GetReadableProperties(sourceType);

            // Build lookup: destination name → source property (after [MapProperty] / [MapIgnore])
            var assignments = new List<(string destPropName, string srcExpression, bool needsCast, IPropertySymbol destProp, IPropertySymbol? srcProp)>();
            var ignoredSrcNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var redirectedSrcNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Index source props by their effective destination name
            var srcByDestName = new Dictionary<string, IPropertySymbol>(StringComparer.OrdinalIgnoreCase);
            foreach (var sp in srcProps)
            {
                if (HasAttribute(sp, MapIgnoreFqn))
                {
                    ignoredSrcNames.Add(sp.Name);
                    continue;
                }

                string effectiveName = sp.Name;
                var mapProp = GetMapPropertyAttribute(sp, MapPropertyFqn);
                if (mapProp is not null)
                {
                    effectiveName = mapProp;
                    redirectedSrcNames.Add(sp.Name); // [MapProperty] redirects this prop — exclude from flattening
                }

                // Last one wins if multiple source props redirect to same dest name
                srcByDestName[effectiveName] = sp;
            }

            // Source props eligible as navigation properties for flattening:
            // not ignored, not [MapProperty]-redirected to a specific dest name
            var navCandidates = new List<IPropertySymbol>();
            foreach (var sp in srcProps)
            {
                if (ignoredSrcNames.Contains(sp.Name)) continue;
                if (redirectedSrcNames.Contains(sp.Name)) continue;
                navCandidates.Add(sp);
            }

            // Collect destination writable properties
            var destProps = GetWritableProperties(destType);

            foreach (var dp in destProps)
            {
                if (!srcByDestName.TryGetValue(dp.Name, out var sp))
                {
                    // Try flattened path: e.g. dest "AddressStreet" ← source.Address.Street
                    var flat = TryFlattenMatch(dp, navCandidates, compilation);
                    if (flat is not null)
                    {
                        assignments.Add((dp.Name, flat.Value.srcExpr, flat.Value.needsCast, dp, null));
                        continue;
                    }

                    // No direct match and no flattened path → EGG2001
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.UnmappedDestinationProperty,
                        Location.None,
                        destType.Name, dp.Name, sourceType.Name));
                    continue;
                }

                bool needsCast = !compilation.ClassifyConversion(sp.Type, dp.Type).IsImplicit;
                if (needsCast && !compilation.ClassifyConversion(sp.Type, dp.Type).IsExplicit)
                {
                    // Incompatible — skip assignment (EGG2001 already emitted by unmapped path isn't applicable here,
                    // but we warn about the type mismatch)
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.TypeMismatch,
                        Location.None,
                        sourceType.Name, sp.Name, sp.Type.ToDisplayString(),
                        destType.Name, dp.Name, dp.Type.ToDisplayString()));
                }
                else if (needsCast)
                {
                    diagnostics.Add(Diagnostic.Create(
                        Diagnostics.TypeMismatch,
                        Location.None,
                        sourceType.Name, sp.Name, sp.Type.ToDisplayString(),
                        destType.Name, dp.Name, dp.Type.ToDisplayString()));
                }

                string srcExpr = needsCast
                    ? $"({dp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})source.{sp.Name}"
                    : $"source.{sp.Name}";

                assignments.Add((dp.Name, srcExpr, needsCast, dp, sp));
            }

            // If there are EGG2001 errors, skip generation (the diagnostics are still emitted)
            bool hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);
            if (hasErrors)
                return (null, diagnostics);

            string ns        = GetNamespace(sourceType);
            string srcFqn    = sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string destFqn   = destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string destShort = destType.Name;
            string extClass  = $"{sourceType.Name}To{destType.Name}Extensions";
            string methodName = $"To{destType.Name}";
            string listMethod = $"To{destType.Name}List";

            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable enable");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
            }

            sb.AppendLine($"    /// <summary>Generated mapping extensions for <see cref=\"{sourceType.Name}\"/> → <see cref=\"{destShort}\"/>.</summary>");
            sb.AppendLine($"    public static partial class {extClass}");
            sb.AppendLine("    {");

            // Single-object method
            sb.AppendLine($"        /// <summary>Maps a <see cref=\"{sourceType.Name}\"/> to <see cref=\"{destShort}\"/>.</summary>");
            sb.AppendLine($"        public static {destFqn} {methodName}(this {srcFqn} source)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (source is null) throw new global::System.ArgumentNullException(nameof(source));");
            sb.AppendLine($"            var destination = new {destFqn}");
            sb.AppendLine("            {");
            foreach (var (destPropName, srcExpr, _, _, _) in assignments)
                sb.AppendLine($"                {destPropName} = {srcExpr},");
            sb.AppendLine("            };");
            sb.AppendLine($"            AfterMap(source, destination);");
            sb.AppendLine("            return destination;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Collection method
            sb.AppendLine($"        /// <summary>Maps an <see cref=\"global::System.Collections.Generic.IEnumerable{{T}}\"/> of <see cref=\"{sourceType.Name}\"/> to a <see cref=\"global::System.Collections.Generic.List{{T}}\"/> of <see cref=\"{destShort}\"/>.</summary>");
            sb.AppendLine($"        public static global::System.Collections.Generic.List<{destFqn}> {listMethod}(this global::System.Collections.Generic.IEnumerable<{srcFqn}> source)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (source is null) throw new global::System.ArgumentNullException(nameof(source));");
            sb.AppendLine($"            var list = source is global::System.Collections.Generic.ICollection<{srcFqn}> c");
            sb.AppendLine($"                ? new global::System.Collections.Generic.List<{destFqn}>(c.Count)");
            sb.AppendLine($"                : new global::System.Collections.Generic.List<{destFqn}>();");
            sb.AppendLine("            foreach (var item in source)");
            sb.AppendLine($"                list.Add(item.{methodName}());");
            sb.AppendLine("            return list;");
            sb.AppendLine("        }");
            sb.AppendLine();

            // Partial hook — users can implement this in their own partial class file
            sb.AppendLine($"        /// <summary>Optional post-map hook. Implement in a partial class file to run custom logic after mapping.</summary>");
            sb.AppendLine($"        static partial void AfterMap({srcFqn} source, {destFqn} destination);");
            sb.AppendLine("    }");

            if (!string.IsNullOrEmpty(ns))
                sb.AppendLine("}");

            return (sb.ToString(), diagnostics);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Tries to resolve a destination property via flattening convention:
        /// <c>destProp.Name = "AddressStreet"</c> → <c>source.Address.Street</c>.
        /// Returns the source expression (with null guard for reference-type nav props)
        /// or null if no flattened path exists.
        /// </summary>
        private static (string srcExpr, bool needsCast)? TryFlattenMatch(
            IPropertySymbol destProp,
            IReadOnlyList<IPropertySymbol> navCandidates,
            Compilation compilation)
        {
            string destName = destProp.Name;

            foreach (var nav in navCandidates)
            {
                if (!destName.StartsWith(nav.Name, StringComparison.OrdinalIgnoreCase))
                    continue;

                string remainder = destName.Substring(nav.Name.Length);
                if (string.IsNullOrEmpty(remainder))
                    continue; // exact name match — handled by direct lookup

                if (nav.Type is not INamedTypeSymbol navType)
                    continue;

                var nested = FindReadableProperty(navType, remainder);
                if (nested == null)
                    continue;

                // Check type compatibility between nested prop and dest prop
                var conv = compilation.ClassifyConversion(nested.Type, destProp.Type);
                if (!conv.IsIdentity && !conv.IsImplicit && !conv.IsExplicit)
                    continue; // incompatible types — try next candidate

                bool needsCast = !conv.IsIdentity && !conv.IsImplicit;
                string castPrefix = needsCast
                    ? $"({destProp.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})"
                    : "";

                // Value-type nav props can never be null — no null guard needed
                bool navCanBeNull = !nav.Type.IsValueType ||
                    (navType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

                string srcExpr = navCanBeNull
                    ? $"source.{nav.Name} != null ? {castPrefix}source.{nav.Name}.{nested.Name} : default"
                    : $"{castPrefix}source.{nav.Name}.{nested.Name}";

                return (srcExpr, needsCast);
            }

            return null;
        }

        /// <summary>Case-insensitive search for a readable public instance property on <paramref name="type"/>.</summary>
        private static IPropertySymbol? FindReadableProperty(INamedTypeSymbol type, string name)
        {
            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var member in t.GetMembers())
                {
                    if (member is IPropertySymbol prop
                        && !prop.IsStatic
                        && !prop.IsIndexer
                        && prop.GetMethod is not null
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return prop;
                    }
                }
                if (t.SpecialType == SpecialType.System_Object) break;
            }
            return null;
        }

        private static IReadOnlyList<IPropertySymbol> GetReadableProperties(INamedTypeSymbol type)
        {
            var result = new List<IPropertySymbol>();
            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var member in t.GetMembers())
                {
                    if (member is IPropertySymbol prop
                        && !prop.IsStatic
                        && !prop.IsIndexer
                        && prop.GetMethod is not null
                        && prop.DeclaredAccessibility == Accessibility.Public)
                    {
                        result.Add(prop);
                    }
                }
                // Stop at object
                if (t.SpecialType == SpecialType.System_Object) break;
            }
            return result;
        }

        private static IReadOnlyList<IPropertySymbol> GetWritableProperties(INamedTypeSymbol type)
        {
            var result = new List<IPropertySymbol>();
            for (var t = type; t is not null; t = t.BaseType)
            {
                foreach (var member in t.GetMembers())
                {
                    if (member is IPropertySymbol prop
                        && !prop.IsStatic
                        && !prop.IsIndexer
                        && prop.DeclaredAccessibility == Accessibility.Public
                        && (prop.SetMethod is not null && prop.SetMethod.DeclaredAccessibility == Accessibility.Public
                            || prop.SetMethod?.IsInitOnly == true))
                    {
                        result.Add(prop);
                    }
                }
                if (t.SpecialType == SpecialType.System_Object) break;
            }
            return result;
        }

        private static bool HasAttribute(ISymbol symbol, string attributeFqn)
        {
            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attributeFqn)
                    return true;
            }
            return false;
        }

        private static string? GetMapPropertyAttribute(ISymbol symbol, string attributeFqn)
        {
            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attributeFqn
                    && attr.ConstructorArguments.Length == 1
                    && attr.ConstructorArguments[0].Value is string dest)
                {
                    return dest;
                }
            }
            return null;
        }

        private static string GetNamespace(INamedTypeSymbol type)
        {
            if (type.ContainingNamespace?.IsGlobalNamespace == true)
                return string.Empty;
            return type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        }
    }

    // ── Data transfer ────────────────────────────────────────────────────────

    internal sealed class MappingRequest
    {
        public string HintName        { get; }
        public string? GeneratedSource { get; }
        public IReadOnlyList<Diagnostic> Diagnostics { get; }

        public MappingRequest(string hintName, string? generatedSource, IReadOnlyList<Diagnostic> diagnostics)
        {
            HintName        = hintName;
            GeneratedSource = generatedSource;
            Diagnostics     = diagnostics;
        }
    }
}

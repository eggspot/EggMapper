using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EggMapper.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UnmappedDestinationMemberAnalyzer : DiagnosticAnalyzer
    {
        private const int MaxClosestMembers = 3;

        private static readonly ImmutableHashSet<string> ChainMethodsThatKeepConventions = ImmutableHashSet.Create(
            "ForMember", "ForPath", "ReverseMap", "ConstructUsing", "BeforeMap", "AfterMap", "MaxDepth", "Validate",
            "IncludeAllDerived");

        private static readonly ImmutableHashSet<string> MemberOptionsThatMapTheMember = ImmutableHashSet.Create(
            "Ignore", "MapFrom", "UseValue", "UseDestinationValue");

        private static readonly ImmutableArray<string> CompilationWideMemberRules = ImmutableArray.Create(
            "ShouldMapProperty", "IncludeAllDerived");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(EggMapperDiagnostics.UnmappedDestinationMember);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                if (UsesCompilationWideMemberRules(start.Compilation)) return;
                start.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private static bool UsesCompilationWideMemberRules(Compilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var text = tree.GetText().ToString();
                foreach (var rule in CompilationWideMemberRules)
                    if (text.IndexOf(rule, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var genericName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax { Name: GenericNameSyntax name } => name,
                GenericNameSyntax name => name,
                _ => null
            };
            if (genericName is null
                || genericName.Identifier.Text != "CreateMap"
                || genericName.TypeArgumentList.Arguments.Count != 2)
                return;

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method
                || !IsEggMapperMethod(method)
                || method.TypeArguments.Length != 2)
                return;

            var sourceType = method.TypeArguments[0];
            var destinationType = method.TypeArguments[1];
            if (!IsCheckableType(sourceType) || !IsCheckableType(destinationType)) return;

            if (!TryReadChain(invocation, out var configuredMembers)) return;

            foreach (var destination in SourceMemberMatcher.WritableProperties(destinationType))
            {
                string? sourceName = null;
                if (configuredMembers.TryGetValue(destination.Name, out var configured))
                {
                    if (configured.IsMapped) continue;
                    sourceName = configured.SourceMemberName;
                }

                if (SourceMemberMatcher.HasReadableProperty(sourceType, sourceName ?? destination.Name)) continue;
                if (SourceMemberMatcher.HasFlattenedSource(destination.Name, sourceType)) continue;

                context.ReportDiagnostic(Diagnostic.Create(
                    EggMapperDiagnostics.UnmappedDestinationMember,
                    genericName.GetLocation(),
                    destinationType.Name,
                    destination.Name,
                    sourceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    destinationType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
                    FixText(context.SemanticModel.Compilation, sourceType, destination)));
            }
        }

        private static string FixText(Compilation compilation, ITypeSymbol sourceType, IPropertySymbol destination)
        {
            var ignore = $".ForMember(d => d.{destination.Name}, o => o.Ignore())";
            var (match, closest) = SourceMemberMatcher.Suggest(compilation, sourceType, destination, MaxClosestMembers);
            if (match != null)
                return $"Fix: .ForMember(d => d.{destination.Name}, o => o.MapFrom(s => s.{match.Path})) or {ignore}";

            var configure = $"Configure: .ForMember(d => d.{destination.Name}, o => o.MapFrom(s => ...)) or {ignore}";
            if (closest.Count == 0) return configure;
            var options = string.Join(", ", closest.Select(c =>
                $"s.{c.Path} ({c.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)})"));
            return $"Options: {options}. {configure}";
        }

        private static bool IsEggMapperMethod(IMethodSymbol method)
        {
            for (var type = method.ContainingType; type != null; type = type.BaseType)
            {
                if (type.ContainingNamespace?.ToDisplayString() == "EggMapper") return true;
                if (type.Interfaces.Any(i => i.ContainingNamespace?.ToDisplayString() == "EggMapper")) return true;
            }
            return false;
        }

        private static bool IsCheckableType(ITypeSymbol type) =>
            type is INamedTypeSymbol named
            && (named.TypeKind == TypeKind.Class || named.TypeKind == TypeKind.Struct)
            && named.SpecialType == SpecialType.None
            && !named.IsUnboundGenericType
            && named.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T
            && !SourceMemberMatcher.IsCollection(named);

        private static bool TryReadChain(
            InvocationExpressionSyntax createMap,
            out Dictionary<string, ConfiguredMember> configuredMembers)
        {
            configuredMembers = new Dictionary<string, ConfiguredMember>(StringComparer.Ordinal);
            ExpressionSyntax top = createMap;
            while (top.Parent is MemberAccessExpressionSyntax access
                   && access.Expression == top
                   && access.Parent is InvocationExpressionSyntax chained)
            {
                var name = access.Name.Identifier.Text;
                if (!ChainMethodsThatKeepConventions.Contains(name)) return false;
                if ((name == "ForMember" || name == "ForPath") && !TryReadMemberConfiguration(chained, configuredMembers))
                    return false;
                top = chained;
            }

            return top.Parent is ExpressionStatementSyntax
                || top.Parent is ArrowExpressionClauseSyntax
                || (top.Parent is LambdaExpressionSyntax lambda && lambda.Body == top);
        }

        private static bool TryReadMemberConfiguration(
            InvocationExpressionSyntax forMember,
            Dictionary<string, ConfiguredMember> configuredMembers)
        {
            var arguments = forMember.ArgumentList.Arguments;
            if (arguments.Count != 2) return false;
            if (!TryReadDestinationMember(arguments[0].Expression, out var memberName)) return false;

            var options = arguments[1].Expression as LambdaExpressionSyntax;
            if (options is null)
            {
                configuredMembers[memberName] = ConfiguredMember.Mapped;
                return true;
            }

            var calls = options.Body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToList();
            var mapFromName = calls
                .Where(c => MethodName(c) == "MapFrom"
                            && c.ArgumentList.Arguments.Count == 1
                            && c.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
                            && literal.IsKind(SyntaxKind.StringLiteralExpression))
                .Select(c => ((LiteralExpressionSyntax)c.ArgumentList.Arguments[0].Expression).Token.ValueText)
                .FirstOrDefault();
            var mapsWithoutName = calls.Any(c =>
            {
                var name = MethodName(c);
                if (name is null || !MemberOptionsThatMapTheMember.Contains(name)) return false;
                return !(name == "MapFrom" && c.ArgumentList.Arguments.Count == 1
                         && c.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax);
            });

            configuredMembers[memberName] = mapsWithoutName
                ? ConfiguredMember.Mapped
                : new ConfiguredMember(false, mapFromName);
            return true;
        }

        private static bool TryReadDestinationMember(ExpressionSyntax expression, out string memberName)
        {
            memberName = "";
            if (expression is not SimpleLambdaExpressionSyntax lambda
                || lambda.Body is not MemberAccessExpressionSyntax access
                || access.Expression is not IdentifierNameSyntax target
                || target.Identifier.Text != lambda.Parameter.Identifier.Text)
                return false;
            memberName = access.Name.Identifier.Text;
            return true;
        }

        private static string? MethodName(InvocationExpressionSyntax invocation) => invocation.Expression switch
        {
            MemberAccessExpressionSyntax access => access.Name.Identifier.Text,
            IdentifierNameSyntax identifier => identifier.Identifier.Text,
            _ => null
        };

        private sealed class ConfiguredMember
        {
            public static readonly ConfiguredMember Mapped = new ConfiguredMember(true, null);

            public ConfiguredMember(bool isMapped, string? sourceMemberName)
            {
                IsMapped = isMapped;
                SourceMemberName = sourceMemberName;
            }

            public bool IsMapped { get; }
            public string? SourceMemberName { get; }
        }
    }
}

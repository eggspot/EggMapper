using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EggMapper.Analyzers
{
    /// <summary>
    /// EGG1003: Suggests replacing a simple <c>CreateMap&lt;TSource, TDest&gt;()</c> call
    /// (one with no customization chains) with a compile-time <c>[MapTo(typeof(TDest))]</c>
    /// attribute declaration via EggMapper.Generator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SuggestCompileTimeMappingAnalyzer : DiagnosticAnalyzer
    {
        // Method names on the fluent builder that indicate non-trivial customization.
        // A CreateMap followed only by .ReverseMap() is still considered "simple".
        private static readonly ImmutableHashSet<string> CustomizationMethods = ImmutableHashSet.Create(
            "ForMember", "ForPath", "ForAllMembers", "ForAllOtherMembers",
            "Ignore", "Condition", "PreCondition", "NullSubstitute", "MapFrom",
            "BeforeMap", "AfterMap", "MaxDepth", "ConstructUsing", "IncludeBase",
            "Include", "Validate");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(EggMapperDiagnostics.SuggestCompileTimeMapping);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext ctx)
        {
            var invocation = (InvocationExpressionSyntax)ctx.Node;

            // Resolve generic name whether called as cfg.CreateMap<S,D>() or CreateMap<S,D>() (in Profile)
            GenericNameSyntax? genericName = null;
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Name is GenericNameSyntax maGeneric)
            {
                genericName = maGeneric;
            }
            else if (invocation.Expression is GenericNameSyntax directGeneric)
            {
                // Implicit-this invocation inside a Profile subclass
                genericName = directGeneric;
            }

            if (genericName is null) return;
            if (genericName.Identifier.Text != "CreateMap") return;
            if (genericName.TypeArgumentList.Arguments.Count != 2) return;

            // Verify it's EggMapper's CreateMap via symbol when possible.
            // If the symbol can't be resolved (error type / missing reference), allow it through
            // based on syntax alone to avoid suppressing diagnostics in partial compilations.
            var symbolInfo = ctx.SemanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is IMethodSymbol method && !IsEggMapperCreateMap(method))
                return;

            // Walk up the syntax tree: if the result of CreateMap<>() is immediately
            // chained with a customization method, don't warn.
            if (HasCustomizationChain(invocation))
                return;

            // Extract type argument names for the diagnostic message
            var typeArgs = genericName.TypeArgumentList.Arguments;
            string srcName = typeArgs[0].ToString();
            string dstName = typeArgs[1].ToString();

            ctx.ReportDiagnostic(Diagnostic.Create(
                EggMapperDiagnostics.SuggestCompileTimeMapping,
                invocation.GetLocation(),
                srcName, dstName));
        }

        private static bool IsEggMapperCreateMap(IMethodSymbol method)
        {
            // Walk up the type hierarchy and interfaces to find one in the EggMapper namespace.
            // This covers: direct call on MapperConfigurationExpression, cfg lambda param,
            // calls inside Profile subclasses (this.CreateMap via Profile base), etc.
            for (var t = method.ContainingType; t is not null; t = t.BaseType)
            {
                if (t.ContainingNamespace?.ToDisplayString() == "EggMapper") return true;
                foreach (var iface in t.Interfaces)
                    if (iface.ContainingNamespace?.ToDisplayString() == "EggMapper") return true;
            }

            // Also check the original definition (for overridden/interface methods)
            if (method.OriginalDefinition?.ContainingType is { } origType)
                if (origType.ContainingNamespace?.ToDisplayString() == "EggMapper") return true;

            return false;
        }

        /// <summary>
        /// Returns true if the invocation is the receiver of a chained customization call.
        /// E.g. <c>cfg.CreateMap&lt;A,B&gt;().ForMember(...)</c> → true.
        /// </summary>
        private static bool HasCustomizationChain(InvocationExpressionSyntax createMapInvocation)
        {
            // The CreateMap<>() invocation is the *receiver* of a chained call when its direct
            // parent is a MemberAccessExpressionSyntax like .ForMember(...).
            // Walk:  createMapInvocation → MemberAccess(.ForMember) → InvocationExpression → ...
            SyntaxNode? node = createMapInvocation;

            while (node?.Parent is MemberAccessExpressionSyntax chainAccess)
            {
                string chainedMethod = chainAccess.Name.Identifier.Text;
                if (CustomizationMethods.Contains(chainedMethod))
                    return true;

                // Advance past the chained invocation
                node = chainAccess.Parent is InvocationExpressionSyntax chainInvocation
                    ? (SyntaxNode)chainInvocation
                    : null;
            }

            return false;
        }
    }
}

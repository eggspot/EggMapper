using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EggMapper.Analyzers
{
    /// <summary>
    /// EGG1002: Warns when <c>MapperConfiguration</c> is constructed without a subsequent
    /// <c>AssertConfigurationIsValid()</c> call anywhere in the same compilation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AssertConfigurationIsValidAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(EggMapperDiagnostics.MissingAssertConfigurationIsValid);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Collect all MapperConfiguration creations and AssertConfigurationIsValid calls,
            // then compare at compilation end.
            context.RegisterCompilationStartAction(RegisterCompilationActions);
        }

        private static void RegisterCompilationActions(CompilationStartAnalysisContext ctx)
        {
            var mapperConfigCreations = new System.Collections.Concurrent.ConcurrentBag<Location>();
            var hasAssertCall = new System.Threading.ManualResetEventSlim(false);

            ctx.RegisterSyntaxNodeAction(nodeCtx =>
            {
                var creation = (ObjectCreationExpressionSyntax)nodeCtx.Node;
                var typeSymbol = nodeCtx.SemanticModel.GetTypeInfo(creation).Type;
                if (typeSymbol is INamedTypeSymbol named &&
                    named.Name == "MapperConfiguration" &&
                    named.ContainingNamespace?.ToDisplayString() == "EggMapper")
                {
                    mapperConfigCreations.Add(creation.GetLocation());
                }
            }, SyntaxKind.ObjectCreationExpression);

            ctx.RegisterSyntaxNodeAction(nodeCtx =>
            {
                var invocation = (InvocationExpressionSyntax)nodeCtx.Node;
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Name.Identifier.Text == "AssertConfigurationIsValid")
                {
                    hasAssertCall.Set();
                }
            }, SyntaxKind.InvocationExpression);

            ctx.RegisterCompilationEndAction(endCtx =>
            {
                if (hasAssertCall.IsSet) return;
                foreach (var location in mapperConfigCreations)
                    endCtx.ReportDiagnostic(Diagnostic.Create(
                        EggMapperDiagnostics.MissingAssertConfigurationIsValid, location));
            });
        }
    }
}

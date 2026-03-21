using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EggMapper.Generator.Tests
{
    /// <summary>Helpers for running the source generator inside unit tests.</summary>
    internal static class GeneratorTestHelper
    {
        /// <summary>
        /// Runs <see cref="MapToGenerator"/> against the provided C# source and returns
        /// all generated source texts and diagnostics.
        /// </summary>
        public static GeneratorRunResult Run(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            };

            var compilation = CSharpCompilation.Create(
                assemblyName: "TestAssembly",
                syntaxTrees:  new[] { syntaxTree },
                references:   references,
                options:      new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var generator = new MapToGenerator();
            var driver = CSharpGeneratorDriver
                .Create(generator)
                .RunGeneratorsAndUpdateCompilation(compilation, out _, out var generatorDiagnostics);

            var result = driver.GetRunResult();
            return new GeneratorRunResult(result, generatorDiagnostics);
        }
    }

    internal sealed class GeneratorRunResult
    {
        private readonly Microsoft.CodeAnalysis.GeneratorDriverRunResult _inner;
        private readonly ImmutableArray<Diagnostic> _extraDiagnostics;

        public GeneratorRunResult(Microsoft.CodeAnalysis.GeneratorDriverRunResult inner, ImmutableArray<Diagnostic> extraDiagnostics)
        {
            _inner = inner;
            _extraDiagnostics = extraDiagnostics;
        }

        /// <summary>All generator-reported diagnostics (from spc.ReportDiagnostic calls).</summary>
        public ImmutableArray<Diagnostic> Diagnostics
        {
            get
            {
                // Use only per-result diagnostics to avoid counting duplicates from the
                // aggregate (GeneratorDriverRunResult.Diagnostics) and the out param.
                var builder = ImmutableArray.CreateBuilder<Diagnostic>();
                foreach (var result in _inner.Results)
                    builder.AddRange(result.Diagnostics);
                return builder.ToImmutable();
            }
        }

        public string? GetSource(string hintNameSuffix)
        {
            foreach (var result in _inner.Results)
            foreach (var source in result.GeneratedSources)
            {
                if (source.HintName.Contains(hintNameSuffix, System.StringComparison.OrdinalIgnoreCase))
                    return source.SourceText.ToString();
            }
            return null;
        }

        public ImmutableArray<Microsoft.CodeAnalysis.Text.SourceText> AllSources()
        {
            var builder = ImmutableArray.CreateBuilder<Microsoft.CodeAnalysis.Text.SourceText>();
            foreach (var result in _inner.Results)
            foreach (var source in result.GeneratedSources)
                builder.Add(source.SourceText);
            return builder.ToImmutable();
        }
    }
}

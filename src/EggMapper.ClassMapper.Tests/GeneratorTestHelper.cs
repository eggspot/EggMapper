using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EggMapper.ClassMapper.Tests
{
    internal sealed class GeneratorRunResult
    {
        private readonly CSharpGeneratorDriver _driver;

        private GeneratorRunResult(CSharpGeneratorDriver driver)
        {
            _driver = driver;
        }

        public static GeneratorRunResult Run(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestProject",
                new[] { syntaxTree },
                refs,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary,
                    nullableContextOptions: NullableContextOptions.Enable));

            var generator = new ClassMapperGenerator();
            var driver = CSharpGeneratorDriver.Create(generator)
                .RunGeneratorsAndUpdateCompilation(
                    compilation,
                    out _,
                    out _);

            return new GeneratorRunResult((CSharpGeneratorDriver)driver);
        }

        /// <summary>Diagnostics reported by the generator itself (EGG3001 etc.).</summary>
        public ImmutableArray<Diagnostic> GeneratorDiagnostics
        {
            get
            {
                var result = _driver.GetRunResult();
                var b = ImmutableArray.CreateBuilder<Diagnostic>();
                foreach (var r in result.Results)
                    b.AddRange(r.Diagnostics);
                return b.ToImmutable();
            }
        }

        /// <summary>Returns the generated source for a hint name that contains <paramref name="hintNamePart"/>.</summary>
        public string? GetSource(string hintNamePart)
        {
            var result = _driver.GetRunResult();
            foreach (var r in result.Results)
                foreach (var s in r.GeneratedSources)
                    if (s.HintName.Contains(hintNamePart))
                        return s.SourceText.ToString();
            return null;
        }
    }
}

using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace EggMapper.Analyzers.Tests
{
    internal static class AnalyzerTestHelper
    {
        /// <summary>
        /// Runs the provided <paramref name="analyzers"/> against <paramref name="source"/>
        /// and returns all reported diagnostics (excluding hidden/info by default based on severity filter).
        /// </summary>
        public static ImmutableArray<Diagnostic> GetDiagnostics(
            string source,
            params DiagnosticAnalyzer[] analyzers)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            // Minimal reference set: mscorlib + System.Runtime + netstandard
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                // Add a fake EggMapper assembly shim so type resolution works
            };

            // Add EggMapper shim inline via a separate compilation
            var shimTree = CSharpSyntaxTree.ParseText(EggMapperShim);
            var shimCompilation = CSharpCompilation.Create(
                "EggMapper",
                new[] { shimTree },
                refs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using var shimStream = new System.IO.MemoryStream();
            var emitResult = shimCompilation.Emit(shimStream);
            shimStream.Seek(0, System.IO.SeekOrigin.Begin);
            var shimRef = MetadataReference.CreateFromStream(shimStream);

            var allRefs = refs.Concat(new[] { shimRef }).ToList();

            var compilation = CSharpCompilation.Create(
                "TestProject",
                new[] { syntaxTree },
                allRefs,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var compilationWithAnalyzers = compilation.WithAnalyzers(
                ImmutableArray.Create(analyzers));

            return compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Minimal EggMapper type shim so the analyzer's symbol resolution works in tests.
        /// </summary>
        private const string EggMapperShim = @"
using System;

namespace EggMapper
{
    public interface IMapperConfigurationExpression
    {
        IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
    }

    public interface IProfileExpression
    {
        IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
    }

    public interface IMappingExpression<TSource, TDestination>
    {
        IMappingExpression<TSource, TDestination> ForMember(string name, Action<object> opts);
        IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action);
        IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);
        IMappingExpression<TDestination, TSource> ReverseMap();
    }

    public class MapperConfiguration
    {
        public MapperConfiguration(Action<IMapperConfigurationExpression> configure) { }
        public void AssertConfigurationIsValid() { }
        public IMapper CreateMapper() => null!;
    }

    public interface IMapper
    {
        TDestination Map<TDestination>(object source);
        TDestination Map<TSource, TDestination>(TSource source);
    }

    public abstract class Profile : IProfileExpression
    {
        public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
            => null!;
    }
}
";
    }
}

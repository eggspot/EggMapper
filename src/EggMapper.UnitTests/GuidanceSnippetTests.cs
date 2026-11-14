using System.Text.RegularExpressions;
using EggMapper;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class GuidanceSnippetTests
{
    private const int MaxLlmsTxtBytes = 6 * 1024;

    private const string Preamble = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using EggMapper;
        global using Microsoft.Extensions.DependencyInjection;

        public class Customer { public string Name { get; set; } = ""; }
        public class OrderItem { public decimal Price { get; set; } }
        public class OrderItemDto { public decimal Price { get; set; } }
        public class Order
        {
            public Customer? Customer { get; set; }
            public List<OrderItem> Items { get; set; } = new();
            public string? Note { get; set; }
        }
        public class OrderDto
        {
            public string CustomerName { get; set; } = "";
            public decimal Total { get; set; }
            public string Internal { get; set; } = "";
            public string Note { get; set; } = "";
        }
        public class Product { public int Id { get; set; } }
        public class ProductDto { public int Id { get; set; } }
        """;

    private static readonly Lazy<MetadataReference[]> References = new(() =>
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Concat(new[]
            {
                typeof(MapperConfiguration).Assembly.Location,
                typeof(IServiceCollection).Assembly.Location
            })
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return paths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray();
    });

    private static IReadOnlyList<string> CompilableSnippets(string file)
    {
        var text = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Guidance", file));
        return Regex.Matches(text, "```csharp compile\r?\n(.*?)```", RegexOptions.Singleline)
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .ToList();
    }

    [Theory]
    [InlineData("llms.txt")]
    [InlineData("llms-full.txt")]
    public void GuidanceSnippets_CompileTogetherAgainstTheRealApi(string file)
    {
        var snippets = CompilableSnippets(file);
        var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(Preamble, path: "preamble") };
        for (var i = 0; i < snippets.Count; i++)
        {
            var snippet = snippets[i];
            var declaresTypes = Regex.IsMatch(snippet, @"^\s*(public\s+)?(sealed\s+)?(class|record)\s", RegexOptions.Multiline);
            var body = declaresTypes
                ? snippet
                : $$"""
                  public static class Snippet{{i + 1}}
                  {
                      public static void Run(Order order, IServiceCollection services, IQueryable<Product> products)
                      {
                  {{snippet}}
                      }
                  }
                  """;
            trees.Add(CSharpSyntaxTree.ParseText(body, path: $"{file}#{i + 1}"));
        }

        var compilation = CSharpCompilation.Create(
            "GuidanceSnippets_" + Guid.NewGuid().ToString("N"),
            trees,
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .ToList();

        errors.Should().BeEmpty($"every ```csharp compile block in {file} must compile against EggMapper");
    }

    [Fact]
    public void LlmsTxt_HasCompilableSnippets()
    {
        CompilableSnippets("llms.txt").Should().NotBeEmpty();
    }

    [Fact]
    public void LlmsTxt_StaysWithinTokenBudget()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Guidance", "llms.txt")).Length;

        bytes.Should().BeLessThanOrEqualTo(MaxLlmsTxtBytes);
    }
}

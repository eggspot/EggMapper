using System.Collections.Immutable;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using EggMapper;
using EggMapper.Analyzers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EggMapper.UnitTests;

public class UnmappedDestinationMemberAnalyzerTests
{
    private const string Usings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using EggMapper;

        """;

    private static readonly Lazy<MetadataReference[]> References = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Concat(new[] { typeof(MapperConfiguration).Assembly.Location, typeof(IServiceCollection).Assembly.Location })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToArray());

    private static CSharpCompilation Compile(string source) =>
        CSharpCompilation.Create(
            "Egg1004_" + Guid.NewGuid().ToString("N"),
            new[] { CSharpSyntaxTree.ParseText(Usings + source) },
            References.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

    private static ImmutableArray<Diagnostic> Analyze(string source)
    {
        var compilation = Compile(source);
        compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        return compilation
            .WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new UnmappedDestinationMemberAnalyzer()))
            .GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
    }

    private static HashSet<string> ReportedMembers(IEnumerable<Diagnostic> diagnostics) =>
        diagnostics.Select(d => Regex.Match(d.GetMessage(), "Destination member '([^']+)'").Groups[1].Value).ToHashSet();

    private static HashSet<string> MembersReportedAtRunTime(string source)
    {
        using var stream = new MemoryStream();
        Compile(source).Emit(stream).Success.Should().BeTrue();
        stream.Position = 0;
        var context = new AssemblyLoadContext("Egg1004Parity", isCollectible: true);
        try
        {
            var assembly = context.LoadFromStream(stream);
            var config = new MapperConfiguration(cfg => cfg.AddMaps(assembly));
            try
            {
                config.AssertConfigurationIsValid();
                return new HashSet<string>();
            }
            catch (InvalidOperationException e)
            {
                return Regex.Matches(e.Message, "Unmapped destination member '([^']+)'")
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet();
            }
        }
        finally
        {
            context.Unload();
        }
    }

    private const string ParitySource = """
        public class Address { public string City { get; set; } = ""; }
        public class Customer { public string Name { get; set; } = ""; public Address Address { get; set; } = new(); }
        public class Order
        {
            public int Id { get; set; }
            public string Note { get; set; } = "";
            public string CustmerName { get; set; } = "";
            public Customer Customer { get; set; } = new();
            public decimal Subtotal { get; set; }
        }
        public class OrderDtoBase { public string BaseMissing { get; set; } = ""; public int Id { get; set; } }
        public class OrderDto : OrderDtoBase
        {
            public string NOTE { get; set; } = "";
            public string CustomerName { get; set; } = "";
            public string CustomerAddressCity { get; set; } = "";
            public string Missing { get; set; } = "";
            public string InitOnly { get; init; } = "";
            public string ReadOnly { get; } = "";
            public string PrivateSet { get; private set; } = "";
            public string Ignored { get; set; } = "";
            public string FromLambda { get; set; } = "";
            public string FromKnownName { get; set; } = "";
            public string FromUnknownName { get; set; } = "";
            public string Fixed { get; set; } = "";
            public string ConditionOnly { get; set; } = "";
            public string ViaPath { get; set; } = "";
            public string BlockBody { get; set; } = "";
        }
        public record OrderSummary(int Id, string Headline);
        public class Product { public int Id { get; set; } }
        public class ProductDto { public int Id { get; set; } public string Missing { get; set; } = ""; }

        public class ParityProfile : Profile
        {
            public ParityProfile()
            {
                CreateMap<Order, OrderDto>()
                    .ForMember(d => d.Ignored, o => o.Ignore())
                    .ForMember(d => d.FromLambda, o => o.MapFrom(s => s.Note + "!"))
                    .ForMember(d => d.FromKnownName, o => o.MapFrom("Note"))
                    .ForMember(d => d.FromUnknownName, o => o.MapFrom("Nope"))
                    .ForMember(d => d.Fixed, o => o.UseValue("x"))
                    .ForMember(d => d.ConditionOnly, o => o.Condition(s => s.Id > 0))
                    .ForPath(d => d.ViaPath, o => o.MapFrom(s => s.Note))
                    .ForMember(d => d.BlockBody, o => { o.Ignore(); });
                CreateMap<Order, OrderSummary>();
                CreateMap<Product, ProductDto>().ConvertUsing(p => new ProductDto { Id = p.Id });
            }
        }
        """;

    [Fact]
    public void Analyzer_ReportsExactlyTheMembersAssertConfigurationIsValidReports()
    {
        var atBuildTime = ReportedMembers(Analyze(ParitySource));
        var atRunTime = MembersReportedAtRunTime(ParitySource);

        atBuildTime.Should().BeEquivalentTo(atRunTime);
        atBuildTime.Should().BeEquivalentTo(new[]
        {
            "OrderDto.BaseMissing", "OrderDto.Missing", "OrderDto.InitOnly", "OrderDto.FromUnknownName",
            "OrderDto.ConditionOnly", "OrderSummary.Headline"
        });
    }

    [Fact]
    public void Analyzer_OneLetterTypo_GivesPasteReadyFix()
    {
        var diagnostics = Analyze("""
            public class Source { public string CustmerName { get; set; } = ""; }
            public class Target { public string CustomerName { get; set; } = ""; }
            public class P : Profile { public P() => CreateMap<Source, Target>(); }
            """);

        var diagnostic = diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("EGG1004");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Be(
            "Destination member 'Target.CustomerName' has no source member in CreateMap<Source, Target>(). " +
            "Fix: .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.CustmerName)) or .ForMember(d => d.CustomerName, o => o.Ignore()).");
        diagnostic.Descriptor.HelpLinkUri.Should().Be("https://eggspot.github.io/EggMapper/diagnostics/EGG1004.html");
    }

    [Fact]
    public void Analyzer_LooseMatches_ListsOptionsAndConfigureLine()
    {
        var diagnostics = Analyze("""
            public class Source { public string AmountText { get; set; } = ""; public decimal AmountValue { get; set; } }
            public class Target { public decimal Amount { get; set; } }
            public static class Setup
            {
                public static MapperConfiguration Build() => new MapperConfiguration(cfg => cfg.CreateMap<Source, Target>());
            }
            """);

        diagnostics.Should().ContainSingle().Which.GetMessage().Should().EndWith(
            "Options: s.AmountValue (decimal), s.AmountText (string). " +
            "Configure: .ForMember(d => d.Amount, o => o.MapFrom(s => ...)) or .ForMember(d => d.Amount, o => o.Ignore()).");
    }

    [Theory]
    [InlineData("var map = CreateMap<Source, Target>(); map.ForMember(d => d.Missing, o => o.Ignore());")]
    [InlineData("CreateMap<Source, Target>().ForAllOtherMembers(o => o.Ignore());")]
    [InlineData("CreateMap<Source, Target>().ConvertUsing(s => new Target());")]
    [InlineData("CreateMap<Source, Target>().IgnoreMissing();")]
    [InlineData("CreateMap<Source, Target>().IncludeBase<object, object>();")]
    [InlineData("CreateMap<Source, Target>().ForMember(d => d.Missing, Options.Ignore);")]
    public void Analyzer_ConfigurationItCannotSee_StaysSilent(string body)
    {
        var diagnostics = Analyze($$"""
            public class Source { public int Id { get; set; } }
            public class Target { public int Id { get; set; } public string Missing { get; set; } = ""; }
            public static class Extensions
            {
                public static IMappingExpression<S, D> IgnoreMissing<S, D>(this IMappingExpression<S, D> map) => map;
            }
            public static class Options
            {
                public static void Ignore(IMemberConfigurationExpression<Source, Target, string> o) => o.Ignore();
            }
            public class P : Profile { public P() { {{body}} } }
            """);

        diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData("cfg.ShouldMapProperty = p => p.Name != \"Missing\";")]
    [InlineData("cfg.CreateMap<object, object>().IncludeAllDerived();")]
    public void Analyzer_CompilationUsesMemberRulesItCannotEvaluate_StaysSilent(string rule)
    {
        var diagnostics = Analyze($$"""
            public class Source { public int Id { get; set; } }
            public class Target { public int Id { get; set; } public string Missing { get; set; } = ""; }
            public static class Setup
            {
                public static MapperConfiguration Build() => new MapperConfiguration(cfg =>
                {
                    {{rule}}
                    cfg.CreateMap<Source, Target>();
                });
            }
            """);

        diagnostics.Should().BeEmpty();
    }
}

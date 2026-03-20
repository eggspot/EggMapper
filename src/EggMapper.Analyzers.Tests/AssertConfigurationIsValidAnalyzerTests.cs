using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EggMapper.Analyzers.Tests
{
    public class AssertConfigurationIsValidAnalyzerTests
    {
        private static readonly AssertConfigurationIsValidAnalyzer Analyzer = new();

        [Fact]
        public void EGG1002_NoAssertCall_ReportsWarning()
        {
            const string source = @"
using EggMapper;

class Startup
{
    void Configure()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<string, string>();
        });
        // Missing: config.AssertConfigurationIsValid();
    }
}";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().ContainSingle(d =>
                d.Id == "EGG1002" &&
                d.Severity == DiagnosticSeverity.Warning);
        }

        [Fact]
        public void EGG1002_WithAssertCall_NoWarning()
        {
            const string source = @"
using EggMapper;

class Startup
{
    void Configure()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<string, string>();
        });
        config.AssertConfigurationIsValid();
    }
}";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().NotContain(d => d.Id == "EGG1002");
        }

        [Fact]
        public void EGG1002_AssertCalledInOtherMethod_NoWarning()
        {
            const string source = @"
using EggMapper;

class Startup
{
    static MapperConfiguration _config = new MapperConfiguration(cfg =>
    {
        cfg.CreateMap<string, string>();
    });

    void Validate()
    {
        _config.AssertConfigurationIsValid();
    }
}";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().NotContain(d => d.Id == "EGG1002");
        }

        [Fact]
        public void EGG1002_NoMapperConfiguration_NoWarning()
        {
            const string source = @"
class MyClass
{
    void DoSomething()
    {
        var x = 42;
    }
}";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().NotContain(d => d.Id == "EGG1002");
        }
    }
}

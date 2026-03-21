using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EggMapper.Analyzers.Tests
{
    public class SuggestCompileTimeMappingAnalyzerTests
    {
        private static readonly SuggestCompileTimeMappingAnalyzer Analyzer = new();

        [Fact]
        public void EGG1003_BareCreateMap_ReportsInfo()
        {
            const string source = @"
using EggMapper;

class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<OrderSource, OrderDest>();
    }
}

class OrderSource { public int Id { get; set; } }
class OrderDest   { public int Id { get; set; } }
";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().ContainSingle(d =>
                d.Id == "EGG1003" &&
                d.Severity == DiagnosticSeverity.Info);
        }

        [Fact]
        public void EGG1003_CreateMapWithForMember_NoInfo()
        {
            const string source = @"
using EggMapper;
using System;

class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<OrderSource, OrderDest>()
            .ForMember(""Name"", o => { });
    }
}

class OrderSource { public int Id { get; set; } }
class OrderDest   { public int Id { get; set; } }
";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().NotContain(d => d.Id == "EGG1003");
        }

        [Fact]
        public void EGG1003_CreateMapWithReverseMapOnly_ReportsInfo()
        {
            // ReverseMap alone is not a customization — still suggest [MapTo]
            const string source = @"
using EggMapper;

class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<OrderSource, OrderDest>().ReverseMap();
    }
}

class OrderSource { public int Id { get; set; } }
class OrderDest   { public int Id { get; set; } }
";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().ContainSingle(d => d.Id == "EGG1003");
        }

        [Fact]
        public void EGG1003_CreateMapWithBeforeMap_NoInfo()
        {
            const string source = @"
using EggMapper;
using System;

class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<OrderSource, OrderDest>()
            .BeforeMap((s, d) => { });
    }
}

class OrderSource { public int Id { get; set; } }
class OrderDest   { public int Id { get; set; } }
";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            diagnostics.Should().NotContain(d => d.Id == "EGG1003");
        }

        [Fact]
        public void EGG1003_DiagnosticContainsTypeNames()
        {
            const string source = @"
using EggMapper;

class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<CustomerSource, CustomerDest>();
    }
}

class CustomerSource { }
class CustomerDest   { }
";
            var diagnostics = AnalyzerTestHelper.GetDiagnostics(source, Analyzer);

            var diag = diagnostics.Should().ContainSingle(d => d.Id == "EGG1003").Subject;
            diag.GetMessage().Should().Contain("CustomerSource");
            diag.GetMessage().Should().Contain("CustomerDest");
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using EggMapper.Analyzers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace EggMapper.Analyzers.Tests
{
    public class DiagnosticDescriptorTests
    {
        private static IEnumerable<DiagnosticDescriptor> AllDescriptors() =>
            new DiagnosticAnalyzer[] { new AssertConfigurationIsValidAnalyzer(), new SuggestCompileTimeMappingAnalyzer() }
                .SelectMany(a => a.SupportedDiagnostics);

        [Fact]
        public void HelpLinks_PointAtTheDiagnosticPageOnTheDocsSite()
        {
            foreach (var descriptor in AllDescriptors())
                descriptor.HelpLinkUri.Should().Be($"https://eggspot.github.io/EggMapper/diagnostics/{descriptor.Id}.html");
        }

        [Fact]
        public void Messages_NameOnlyApisThatExistInEggMapper()
        {
            foreach (var descriptor in AllDescriptors())
                descriptor.MessageFormat.ToString().Should().NotContain("ConfigurationProvider");
        }
    }
}

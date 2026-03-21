using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EggMapper.ClassMapper.Tests
{
    public class ClassMapperGeneratorTests
    {
        // ── EGG3002: no partial methods ───────────────────────────────────────────

        [Fact]
        public void EGG3002_NoPartialMethods_ReportsWarning()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class EmptyMapper { }
";
            var result = GeneratorRunResult.Run(source);

            result.GeneratorDiagnostics.Should().ContainSingle(d =>
                d.Id == "EGG3002" && d.Severity == DiagnosticSeverity.Warning);
        }

        // ── Attribute injection ───────────────────────────────────────────────────

        [Fact]
        public void AttributeIsInjected_IntoCompilation()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
}

public class Order { public int Id { get; set; } }
public class OrderDto { public int Id { get; set; } }
";
            var result = GeneratorRunResult.Run(source);

            result.GetSource("EggMapperAttribute.g.cs").Should().NotBeNull();
        }

        // ── Flat mapping ──────────────────────────────────────────────────────────

        [Fact]
        public void FlatMapping_GeneratesPropertyAssignments()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
}

public class Order   { public int Id { get; set; } public string Name { get; set; } = """"; }
public class OrderDto{ public int Id { get; set; } public string Name { get; set; } = """"; }
";
            var result = GeneratorRunResult.Run(source);

            var generated = result.GetSource("OrderMapper.g.cs");
            generated.Should().NotBeNull();
            generated.Should().Contain("Id = source.Id,");
            generated.Should().Contain("Name = source.Name,");
        }

        // ── Static Instance property ──────────────────────────────────────────────

        [Fact]
        public void InstanceProperty_IsGenerated()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
}

public class Order   { public int Id { get; set; } }
public class OrderDto{ public int Id { get; set; } }
";
            var result = GeneratorRunResult.Run(source);
            var generated = result.GetSource("OrderMapper.g.cs");

            generated.Should().Contain("public static OrderMapper Instance");
        }

        // ── Enum mapping ──────────────────────────────────────────────────────────

        [Fact]
        public void EnumMapping_GeneratesExplicitCast()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class StatusMapper
{
    public partial StatusDto Map(Status source);
}

public class Status   { public StatusKind Kind { get; set; } }
public class StatusDto{ public StatusDtoKind Kind { get; set; } }

public enum StatusKind    { Active, Inactive }
public enum StatusDtoKind { Active, Inactive }
";
            var result = GeneratorRunResult.Run(source);
            var generated = result.GetSource("StatusMapper.g.cs");

            generated.Should().Contain("(StatusDtoKind)source.Kind");
        }

        // ── Nested type mapping ───────────────────────────────────────────────────

        [Fact]
        public void NestedMapping_UsesOtherPartialMethod()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
    public partial AddressDto Map(Address source);
}

public class Order    { public Address Address { get; set; } = new(); }
public class OrderDto { public AddressDto Address { get; set; } = new(); }
public class Address    { public string Street { get; set; } = """"; }
public class AddressDto { public string Street { get; set; } = """"; }
";
            var result = GeneratorRunResult.Run(source);
            var generated = result.GetSource("OrderMapper.g.cs");

            // The Address property should be mapped via the other partial method.
            generated.Should().Contain("Address = Map(source.Address)");
        }

        // ── EGG3001: unmapped property ────────────────────────────────────────────

        [Fact]
        public void EGG3001_UnmappedDestProperty_ReportsWarning()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class Mapper
{
    public partial Dest Map(Source source);
}

public class Source { public int Id { get; set; } }
public class Dest   { public int Id { get; set; } public string Extra { get; set; } = """"; }
";
            var result = GeneratorRunResult.Run(source);

            result.GeneratorDiagnostics.Should().ContainSingle(d =>
                d.Id == "EGG3001" && d.Severity == DiagnosticSeverity.Warning);
        }

        // ── Custom converter method ───────────────────────────────────────────────

        [Fact]
        public void CustomConverterMethod_IsUsedForIncompatibleTypes()
        {
            // Test the converter path with incompatible types (DateTime→string).
            const string sourceWithConv = @"
using EggMapper;
using System;

[EggMapper]
public partial class PersonMapper
{
    public partial PersonDto Map(Person source);

    private string FormatBirthday(DateTime d) => d.ToString(""yyyy-MM-dd"");
}

public class Person    { public DateTime Birthday { get; set; } }
public class PersonDto { public string Birthday { get; set; } = """"; }
";
            var result = GeneratorRunResult.Run(sourceWithConv);
            var generated = result.GetSource("PersonMapper.g.cs");

            generated.Should().Contain("FormatBirthday(source.Birthday)");
        }

        // ── Collection mapping ────────────────────────────────────────────────────

        [Fact]
        public void CollectionMapping_GeneratesSelectToList()
        {
            const string source = @"
using EggMapper;
using System.Collections.Generic;

[EggMapper]
public partial class OrderMapper
{
    public partial OrderDto Map(Order source);
    public partial LineDto Map(Line source);
}

public class Order    { public List<Line> Lines { get; set; } = new(); }
public class OrderDto { public List<LineDto> Lines { get; set; } = new(); }
public class Line    { public int Qty { get; set; } }
public class LineDto { public int Qty { get; set; } }
";
            var result = GeneratorRunResult.Run(source);
            var generated = result.GetSource("OrderMapper.g.cs");

            generated.Should().Contain("Lines?.Select(Map).ToList()");
        }

        // ── Multiple partial methods ──────────────────────────────────────────────

        [Fact]
        public void MultiplePartialMethods_AllImplemented()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class BidirectionalMapper
{
    public partial ADto Map(A source);
    public partial A    Map(ADto source);
}

public class A    { public int X { get; set; } }
public class ADto { public int X { get; set; } }
";
            var result = GeneratorRunResult.Run(source);
            var generated = result.GetSource("BidirectionalMapper.g.cs");

            generated.Should().Contain("partial ADto Map(A source)");
            generated.Should().Contain("partial A Map(ADto source)");
        }

        // ── Global namespace ──────────────────────────────────────────────────────

        [Fact]
        public void GlobalNamespace_NoNamespaceWrapper()
        {
            const string source = @"
using EggMapper;

[EggMapper]
public partial class SimpleMapper
{
    public partial Dest Map(Src source);
}

public class Src  { public int V { get; set; } }
public class Dest { public int V { get; set; } }
";
            var result = GeneratorRunResult.Run(source);
            var generated = result.GetSource("SimpleMapper.g.cs");

            // Global namespace: no "namespace {" wrapper.
            generated.Should().NotContain("namespace ");
            generated.Should().Contain("public partial class SimpleMapper");
        }
    }
}

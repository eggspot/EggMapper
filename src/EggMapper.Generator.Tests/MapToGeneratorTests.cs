using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace EggMapper.Generator.Tests
{
    public class MapToGeneratorTests
    {
        // ── Attribute injection ──────────────────────────────────────────────

        [Fact]
        public void Generator_AlwaysInjectsAttributeSource()
        {
            var result = GeneratorTestHelper.Run("// empty");

            var sources = result.AllSources();
            sources.Should().Contain(s => s.ToString().Contains("class MapToAttribute"));
            sources.Should().Contain(s => s.ToString().Contains("class MapPropertyAttribute"));
            sources.Should().Contain(s => s.ToString().Contains("class MapIgnoreAttribute"));
        }

        // ── Basic flat mapping ────────────────────────────────────────────────

        [Fact]
        public void Generator_FlatMapping_GeneratesExtensionMethod()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(OrderDto))]
    public class Order
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = """";
        public decimal Total { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = """";
        public decimal Total { get; set; }
    }
}";

            var result = GeneratorTestHelper.Run(source);

            result.Diagnostics.Should().BeEmpty();
            var generated = result.GetSource("OrderToOrderDtoExtensions");
            generated.Should().NotBeNull();
            generated.Should().Contain("public static global::MyApp.OrderDto ToOrderDto");
            generated.Should().Contain("Id = source.Id");
            generated.Should().Contain("CustomerName = source.CustomerName");
            generated.Should().Contain("Total = source.Total");
        }

        [Fact]
        public void Generator_FlatMapping_GeneratesListExtensionMethod()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(OrderDto))]
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
    }
}";

            var result = GeneratorTestHelper.Run(source);

            result.Diagnostics.Should().BeEmpty();
            var generated = result.GetSource("OrderToOrderDtoExtensions");
            generated.Should().Contain("ToOrderDtoList");
            generated.Should().Contain("IEnumerable");
            generated.Should().Contain("List<");
        }

        // ── [MapProperty] override ────────────────────────────────────────────

        [Fact]
        public void Generator_MapProperty_RedirectsToDestinationName()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(CustomerDto))]
    public class Customer
    {
        public int Id { get; set; }

        [MapProperty(""FullName"")]
        public string Name { get; set; } = """";
    }

    public class CustomerDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = """";
    }
}";

            var result = GeneratorTestHelper.Run(source);

            result.Diagnostics.Should().BeEmpty();
            var generated = result.GetSource("CustomerToCustomerDtoExtensions");
            generated.Should().Contain("FullName = source.Name");
        }

        // ── [MapIgnore] ───────────────────────────────────────────────────────

        [Fact]
        public void Generator_MapIgnore_SkipsSourceProperty()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(UserDto))]
    public class User
    {
        public int Id { get; set; }

        [MapIgnore]
        public string PasswordHash { get; set; } = """";
    }

    public class UserDto
    {
        public int Id { get; set; }
    }
}";

            var result = GeneratorTestHelper.Run(source);

            result.Diagnostics.Should().BeEmpty();
            var generated = result.GetSource("UserToUserDtoExtensions");
            generated.Should().NotContain("PasswordHash");
        }

        // ── EGG2001: unmapped destination property ────────────────────────────

        [Fact]
        public void Generator_UnmappedDestinationProperty_EmitsEGG2001()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(ProductDto))]
    public class Product
    {
        public int Id { get; set; }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = """";  // No matching source property
    }
}";

            var result = GeneratorTestHelper.Run(source);

            result.Diagnostics.Should().ContainSingle(d =>
                d.Id == "EGG2001" &&
                d.Severity == DiagnosticSeverity.Error);
        }

        // ── AfterMap partial hook ─────────────────────────────────────────────

        [Fact]
        public void Generator_GeneratesAfterMapPartialHook()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(OrderDto))]
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
    }
}";

            var result = GeneratorTestHelper.Run(source);

            var generated = result.GetSource("OrderToOrderDtoExtensions");
            generated.Should().Contain("static partial void AfterMap(");
            generated.Should().Contain("AfterMap(source, destination)");
        }

        // ── Multiple [MapTo] on same source type ──────────────────────────────

        [Fact]
        public void Generator_MultipleMapTo_GeneratesMethodsForBothDestinations()
        {
            const string source = @"
using EggMapper;

namespace MyApp
{
    [MapTo(typeof(OrderDto))]
    [MapTo(typeof(OrderSummary))]
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderDto     { public int Id { get; set; } }
    public class OrderSummary { public int Id { get; set; } }
}";

            var result = GeneratorTestHelper.Run(source);

            result.Diagnostics.Should().BeEmpty();
            // Both destinations end up in the same hint file
            var allText = string.Join("\n", result.AllSources().Select(s => s.ToString()));
            allText.Should().Contain("ToOrderDto");
            allText.Should().Contain("ToOrderSummary");
        }

        // ── Partial class is generated in correct namespace ───────────────────

        [Fact]
        public void Generator_GeneratesCodeInSourceTypeNamespace()
        {
            const string source = @"
using EggMapper;

namespace Acme.Orders
{
    [MapTo(typeof(OrderDto))]
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
    }
}";

            var result = GeneratorTestHelper.Run(source);

            var generated = result.GetSource("OrderToOrderDtoExtensions");
            generated.Should().Contain("namespace Acme.Orders");
        }

        // ── Global namespace (no namespace) ──────────────────────────────────

        [Fact]
        public void Generator_GlobalNamespace_GeneratesWithoutNamespaceBlock()
        {
            const string source = @"
using EggMapper;

[MapTo(typeof(OrderDto))]
public class Order
{
    public int Id { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
}
";

            var result = GeneratorTestHelper.Run(source);

            var generated = result.GetSource("OrderToOrderDtoExtensions");
            generated.Should().NotContain("namespace ");
        }
    }
}

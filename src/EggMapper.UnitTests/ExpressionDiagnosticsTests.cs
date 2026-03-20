using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ExpressionDiagnosticsTests
{
    [Fact]
    public void GetMappingExpressionText_returns_non_empty_string_for_simple_map()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>());

        var text = config.GetMappingExpressionText<FlatSource, FlatDest>();

        text.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetMappingExpressionText_contains_src_parameter()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>());

        var text = config.GetMappingExpressionText<FlatSource, FlatDest>();

        text.Should().Contain("src");
        text.Should().Contain("=>");
    }

    [Fact]
    public void GetMappingExpressionText_returns_null_for_unregistered_map()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>());

        var text = config.GetMappingExpressionText<PersonSourceSimple, FlatDest>();

        text.Should().BeNull();
    }

    [Fact]
    public void GetMappingExpressionText_is_a_lambda_expression_string()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlatSource, FlatDest>());

        var text = config.GetMappingExpressionText<FlatSource, FlatDest>();

        // Expression.ToString() produces a lambda-style representation
        text.Should().StartWith("(");
        text.Should().Contain("=>");
    }

    [Fact]
    public void GetMappingExpressionText_works_for_nested_maps()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<AddressSource, AddressDest>();
            cfg.CreateMap<PersonSource, PersonDest>();
        });

        var text = config.GetMappingExpressionText<PersonSource, PersonDest>();
        text.Should().NotBeNullOrEmpty();
    }
}

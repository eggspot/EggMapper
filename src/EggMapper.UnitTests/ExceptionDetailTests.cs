using EggMapper;
using EggMapper.Internal;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

/// <summary>
/// Verifies that exception messages show readable type names and precise context
/// so users can quickly identify what went wrong.
/// </summary>
public class ExceptionDetailTests
{
    // ── TypeNameHelper.Readable ──────────────────────────────────────────

    [Fact]
    public void Readable_SimpleType_ReturnsName()
    {
        TypeNameHelper.Readable(typeof(string)).Should().Be("String");
        TypeNameHelper.Readable(typeof(int)).Should().Be("Int32");
    }

    [Fact]
    public void Readable_GenericList_ShowsElementType()
    {
        TypeNameHelper.Readable(typeof(List<string>)).Should().Be("List<String>");
        TypeNameHelper.Readable(typeof(List<int>)).Should().Be("List<Int32>");
    }

    [Fact]
    public void Readable_GenericDictionary_ShowsBothTypes()
    {
        TypeNameHelper.Readable(typeof(Dictionary<string, int>))
            .Should().Be("Dictionary<String, Int32>");
    }

    [Fact]
    public void Readable_NestedGeneric_ShowsFullDepth()
    {
        TypeNameHelper.Readable(typeof(List<List<string>>))
            .Should().Be("List<List<String>>");
    }

    [Fact]
    public void Readable_Nullable_ShowsQuestionMark()
    {
        TypeNameHelper.Readable(typeof(int?)).Should().Be("Int32?");
        TypeNameHelper.Readable(typeof(DateTime?)).Should().Be("DateTime?");
    }

    [Fact]
    public void Readable_Array_ShowsBrackets()
    {
        TypeNameHelper.Readable(typeof(int[])).Should().Be("Int32[]");
        TypeNameHelper.Readable(typeof(string[])).Should().Be("String[]");
    }

    [Fact]
    public void Pair_ShowsArrow()
    {
        TypeNameHelper.Pair(typeof(List<string>), typeof(List<int>))
            .Should().Be("List<String> -> List<Int32>");
    }

    // ── Error message content ────────────────────────────────────────────

    private class SourceA { public int Id { get; set; } }
    private class DestB { public int Id { get; set; } }

    [Fact]
    public void NoMapping_Collection_ShowsElementTypes()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        // Map<List<A>, List<B>> with no element map → tells you which element CreateMap is missing
        var act = () => mapper.Map<List<SourceA>, List<DestB>>(new List<SourceA>());

        act.Should().Throw<MappingException>()
            .WithMessage("*List<SourceA> -> List<DestB>*")
            .WithInnerException<InvalidOperationException>()
            .WithMessage("*CreateMap<SourceA, DestB>*");
    }

    [Fact]
    public void NoMapping_SingleTypeArg_ShowsReadableNames()
    {
        var mapper = new MapperConfiguration(cfg => { }).CreateMapper();

        var act = () => mapper.Map<DestB>(new SourceA());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*SourceA -> DestB*");
    }

    [Fact]
    public void MappingException_ShowsReadableNames()
    {
        var ex = new MappingException(
            typeof(List<SourceA>), typeof(List<DestB>),
            new NullReferenceException("test"));

        ex.Message.Should().Contain("List<SourceA> -> List<DestB>");
        ex.Message.Should().NotContain("`1");
    }

    [Fact]
    public void MappingException_WithMember_ShowsPropertyName()
    {
        var ex = new MappingException(
            typeof(SourceA), typeof(DestB),
            "SomeProperty",
            new NullReferenceException("was null"));

        ex.Message.Should().Contain("SourceA -> DestB");
        ex.Message.Should().Contain("member 'SomeProperty'");
        ex.Message.Should().Contain("was null");
    }
}

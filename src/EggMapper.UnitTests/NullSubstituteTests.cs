using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class NullSubstituteTests
{
    [Fact]
    public void NullSubstitute_WhenSourceIsNull_UsesSubstitute()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullSubSrc, NullSubDst>()
               .ForMember(d => d.Name, o =>
               {
                   o.MapFrom(s => s.Name);
                   o.NullSubstitute("Default");
               }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<NullSubSrc, NullSubDst>(new NullSubSrc { Name = null });
        result.Name.Should().Be("Default");
    }

    [Fact]
    public void NullSubstitute_WhenSourceIsNotNull_UsesSourceValue()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullSubSrc, NullSubDst>()
               .ForMember(d => d.Name, o =>
               {
                   o.MapFrom(s => s.Name);
                   o.NullSubstitute("Default");
               }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<NullSubSrc, NullSubDst>(new NullSubSrc { Name = "Actual" });
        result.Name.Should().Be("Actual");
    }

    [Fact]
    public void NullSubstitute_WithCustomResolver_UsesSubstitute()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullSubSrc, NullSubDst>()
               .ForMember(d => d.Name, o =>
               {
                   o.MapFrom((s, _) => s.Name);
                   o.NullSubstitute("Fallback");
               }));
        var mapper = config.CreateMapper();

        var result = mapper.Map<NullSubSrc, NullSubDst>(new NullSubSrc { Name = null });
        result.Name.Should().Be("Fallback");
    }
}

file class NullSubSrc { public string? Name { get; set; } }
file class NullSubDst { public string Name { get; set; } = ""; }

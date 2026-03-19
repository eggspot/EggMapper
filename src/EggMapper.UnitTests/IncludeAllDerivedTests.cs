using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class IncludeAllDerivedTests
{
    [Fact]
    public void IncludeAllDerived_MapsBaseProperties()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<IadBase, IadBaseDest>()
               .ForMember(d => d.BaseName, o => o.MapFrom(s => s.Name))
               .IncludeAllDerived();
            cfg.CreateMap<IadDerived, IadDerivedDest>();
        });
        var mapper = config.CreateMapper();

        var src = new IadDerived { Name = "Base", Extra = "More" };
        var result = mapper.Map<IadDerived, IadDerivedDest>(src);
        result.BaseName.Should().Be("Base");
        result.Extra.Should().Be("More");
    }

    [Fact]
    public void IncludeAllDerived_WorksWithMultipleDerivedTypes()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<IadBase, IadBaseDest>()
               .ForMember(d => d.BaseName, o => o.MapFrom(s => s.Name))
               .IncludeAllDerived();
            cfg.CreateMap<IadDerived, IadDerivedDest>();
            cfg.CreateMap<IadDerived2, IadDerived2Dest>();
        });
        var mapper = config.CreateMapper();

        var result1 = mapper.Map<IadDerived, IadDerivedDest>(new IadDerived { Name = "D1", Extra = "E1" });
        var result2 = mapper.Map<IadDerived2, IadDerived2Dest>(new IadDerived2 { Name = "D2", Other = 99 });

        result1.BaseName.Should().Be("D1");
        result2.BaseName.Should().Be("D2");
        result2.Other.Should().Be(99);
    }
}

file class IadBase { public string Name { get; set; } = ""; }
file class IadDerived : IadBase { public string Extra { get; set; } = ""; }
file class IadDerived2 : IadBase { public int Other { get; set; } }
file class IadBaseDest { public string BaseName { get; set; } = ""; }
file class IadDerivedDest : IadBaseDest { public string Extra { get; set; } = ""; }
file class IadDerived2Dest : IadBaseDest { public int Other { get; set; } }

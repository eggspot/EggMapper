using EggMapper;
using EggMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

file class EnumIntSource { public int Code { get; set; } }
file class EnumIntDest { public Status Code { get; set; } }
file class StatusToIntSource { public Status Status { get; set; } }
file class StatusToIntDest { public int Status { get; set; } }
file class FlagsSource { public Permissions Perms { get; set; } }
file class FlagsDest { public Permissions Perms { get; set; } }

public class EnumerationTests
{
    [Fact]
    public void Same_enum_type_maps_correctly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EnumSource, EnumDest>()).CreateMapper();

        var src = new EnumSource { Status = Status.Pending };
        var dest = mapper.Map<EnumSource, EnumDest>(src);
        dest.Status.Should().Be(Status.Pending);
    }

    [Fact]
    public void All_enum_values_map_correctly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EnumSource, EnumDest>()).CreateMapper();

        foreach (var val in Enum.GetValues<Status>())
        {
            var src = new EnumSource { Status = val };
            var dest = mapper.Map<EnumSource, EnumDest>(src);
            dest.Status.Should().Be(val);
        }
    }

    [Fact]
    public void Enum_to_int_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StatusToIntSource, StatusToIntDest>()).CreateMapper();

        var src = new StatusToIntSource { Status = Status.Inactive };
        var dest = mapper.Map<StatusToIntSource, StatusToIntDest>(src);
        dest.Status.Should().Be(1);
    }

    [Fact]
    public void Int_to_enum_conversion_via_explicit_cast()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<EnumIntSource, EnumIntDest>()
               .ForMember(d => d.Code, opts => opts.MapFrom(s => (Status)s.Code));
        }).CreateMapper();

        var src = new EnumIntSource { Code = 2 };
        var dest = mapper.Map<EnumIntSource, EnumIntDest>(src);
        dest.Code.Should().Be(Status.Pending);
    }

    [Fact]
    public void Flags_enum_maps_correctly()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<FlagsSource, FlagsDest>()).CreateMapper();

        var src = new FlagsSource { Perms = Permissions.Read | Permissions.Write };
        var dest = mapper.Map<FlagsSource, FlagsDest>(src);
        dest.Perms.Should().Be(Permissions.Read | Permissions.Write);
        dest.Perms.HasFlag(Permissions.Read).Should().BeTrue();
        dest.Perms.HasFlag(Permissions.Admin).Should().BeFalse();
    }
}

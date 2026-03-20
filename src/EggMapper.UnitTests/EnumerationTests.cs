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
file class EnumToStringSource { public Status Status { get; set; } }
file class EnumToStringDest { public string Status { get; set; } = ""; }
file class StringToEnumSource { public string Status { get; set; } = ""; }
file class StringToEnumDest { public Status Status { get; set; } }
file class NullableEnumToStringSource { public Status? Status { get; set; } }
file class NullableEnumToStringDest { public string? Status { get; set; } }
file class StringToNullableEnumSource { public string? Status { get; set; } }
file class StringToNullableEnumDest { public Status? Status { get; set; } }

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

    [Fact]
    public void Enum_to_string_auto_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EnumToStringSource, EnumToStringDest>()).CreateMapper();

        var src = new EnumToStringSource { Status = Status.Active };
        var dest = mapper.Map<EnumToStringSource, EnumToStringDest>(src);
        dest.Status.Should().Be("Active");
    }

    [Fact]
    public void Enum_to_string_all_values()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<EnumToStringSource, EnumToStringDest>()).CreateMapper();

        mapper.Map<EnumToStringSource, EnumToStringDest>(new EnumToStringSource { Status = Status.Active }).Status.Should().Be("Active");
        mapper.Map<EnumToStringSource, EnumToStringDest>(new EnumToStringSource { Status = Status.Inactive }).Status.Should().Be("Inactive");
        mapper.Map<EnumToStringSource, EnumToStringDest>(new EnumToStringSource { Status = Status.Pending }).Status.Should().Be("Pending");
    }

    [Fact]
    public void String_to_enum_auto_conversion()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StringToEnumSource, StringToEnumDest>()).CreateMapper();

        var src = new StringToEnumSource { Status = "Pending" };
        var dest = mapper.Map<StringToEnumSource, StringToEnumDest>(src);
        dest.Status.Should().Be(Status.Pending);
    }

    [Fact]
    public void String_to_enum_case_insensitive()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StringToEnumSource, StringToEnumDest>()).CreateMapper();

        var src = new StringToEnumSource { Status = "active" };
        var dest = mapper.Map<StringToEnumSource, StringToEnumDest>(src);
        dest.Status.Should().Be(Status.Active);
    }

    [Fact]
    public void String_to_enum_empty_string_returns_default()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StringToEnumSource, StringToEnumDest>()).CreateMapper();

        var src = new StringToEnumSource { Status = "" };
        var dest = mapper.Map<StringToEnumSource, StringToEnumDest>(src);
        dest.Status.Should().Be(default(Status));
    }

    [Fact]
    public void Nullable_enum_to_string_with_value()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableEnumToStringSource, NullableEnumToStringDest>()).CreateMapper();

        var src = new NullableEnumToStringSource { Status = Status.Inactive };
        var dest = mapper.Map<NullableEnumToStringSource, NullableEnumToStringDest>(src);
        dest.Status.Should().Be("Inactive");
    }

    [Fact]
    public void Nullable_enum_to_string_when_null()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<NullableEnumToStringSource, NullableEnumToStringDest>()).CreateMapper();

        var src = new NullableEnumToStringSource { Status = null };
        var dest = mapper.Map<NullableEnumToStringSource, NullableEnumToStringDest>(src);
        dest.Status.Should().BeNull();
    }

    [Fact]
    public void String_to_nullable_enum_with_value()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StringToNullableEnumSource, StringToNullableEnumDest>()).CreateMapper();

        var src = new StringToNullableEnumSource { Status = "Active" };
        var dest = mapper.Map<StringToNullableEnumSource, StringToNullableEnumDest>(src);
        dest.Status.Should().Be(Status.Active);
    }

    [Fact]
    public void String_to_nullable_enum_when_null()
    {
        var mapper = new MapperConfiguration(cfg =>
            cfg.CreateMap<StringToNullableEnumSource, StringToNullableEnumDest>()).CreateMapper();

        var src = new StringToNullableEnumSource { Status = null };
        var dest = mapper.Map<StringToNullableEnumSource, StringToNullableEnumDest>(src);
        dest.Status.Should().BeNull();
    }
}

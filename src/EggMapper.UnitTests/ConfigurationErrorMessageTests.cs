using EggMapper;
using FluentAssertions;
using Xunit;

namespace EggMapper.UnitTests;

public class ConfigurationErrorMessageTests
{
    public class Address { public string City { get; set; } = ""; }

    public class OrderSource
    {
        public string CustmerName { get; set; } = "";
        public string Note { get; set; } = "";
        public Address Address { get; set; } = new();
        public string AmountText { get; set; } = "";
        public decimal AmountValue { get; set; }
    }

    public class OrderTarget
    {
        public string CustomerName { get; set; } = "";
        public string Notes { get; set; } = "";
        public string City { get; set; } = "";
        public decimal Amount { get; set; }
        public Guid TrackingId { get; set; }
    }

    public class Narrow { public string Name { get; set; } = ""; }

    public class Wide
    {
        public string Name { get; set; } = "";
        public string Extra { get; set; } = "";
    }

    public class ManyMembers
    {
        public string M01 { get; set; } = ""; public string M02 { get; set; } = ""; public string M03 { get; set; } = "";
        public string M04 { get; set; } = ""; public string M05 { get; set; } = ""; public string M06 { get; set; } = "";
        public string M07 { get; set; } = ""; public string M08 { get; set; } = ""; public string M09 { get; set; } = "";
        public string M10 { get; set; } = ""; public string M11 { get; set; } = ""; public string M12 { get; set; } = "";
    }

    public class TwoTypos
    {
        public string Sendar { get; set; } = "";
        public string Sendor { get; set; } = "";
    }

    public class OneMember { public string Sender { get; set; } = ""; }

    public static class Sales { public class Dto { public string Code { get; set; } = ""; } }
    public static class Billing { public class Dto { public string Code { get; set; } = ""; } }

    public class ForwardProfile : Profile
    {
        public ForwardProfile() => CreateMap<Wide, Narrow>().ReverseMap();
    }

    public class ExplicitReverseProfile : Profile
    {
        public ExplicitReverseProfile() => CreateMap<Narrow, Wide>().ForMember(d => d.Extra, o => o.Ignore());
    }

    private static string ValidationMessage(Action<IMapperConfigurationExpression> configure)
    {
        var config = new MapperConfiguration(configure);
        var act = () => config.AssertConfigurationIsValid();
        return act.Should().Throw<InvalidOperationException>().Which.Message;
    }

    [Fact]
    public void AssertConfigurationIsValid_UnmappedMembers_StartsWithTotalsAndKeepsLegacyLine()
    {
        var message = ValidationMessage(cfg =>
        {
            cfg.CreateMap<OrderSource, OrderTarget>();
            cfg.CreateMap<Narrow, Wide>();
        });

        message.Should().StartWith("EggMapper configuration is invalid: 6 unmapped destination members in 2 maps.");
        message.Should().Contain("Unmapped destination member 'Wide.Extra' in map Narrow -> Wide");
        message.Should().Contain("\n\nNarrow -> Wide\n");
        message.IndexOf("Narrow -> Wide\n", StringComparison.Ordinal)
            .Should().BeLessThan(message.IndexOf("OrderSource -> OrderTarget\n", StringComparison.Ordinal));
    }

    [Fact]
    public void AssertConfigurationIsValid_OneLetterTypo_SuggestsMapFromFix()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<OrderSource, OrderTarget>());

        message.Should().Contain("Fix: .ForMember(d => d.CustomerName, o => o.MapFrom(s => s.CustmerName))");
        message.Should().Contain("or: .ForMember(d => d.CustomerName, o => o.Ignore())");
    }

    [Fact]
    public void AssertConfigurationIsValid_SingularSourceForPluralDestination_SuggestsMapFromFix()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<OrderSource, OrderTarget>());

        message.Should().Contain("Fix: .ForMember(d => d.Notes, o => o.MapFrom(s => s.Note))");
    }

    [Fact]
    public void AssertConfigurationIsValid_SingleNestedMemberWithSameName_SuggestsNestedPath()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<OrderSource, OrderTarget>());

        message.Should().Contain("Fix: .ForMember(d => d.City, o => o.MapFrom(s => s.Address.City))");
    }

    [Fact]
    public void AssertConfigurationIsValid_LooseMatches_ListsTypeCompatibleOptionsFirstWithoutFix()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<OrderSource, OrderTarget>());

        message.Should().Contain("Options: s.AmountValue (Decimal), s.AmountText (String)");
        message.Should().Contain("Configure: .ForMember(d => d.Amount, o => o.MapFrom(s => ...)) or .ForMember(d => d.Amount, o => o.Ignore())");
        message.Should().NotContain("Fix: .ForMember(d => d.Amount,");
    }

    [Fact]
    public void AssertConfigurationIsValid_NoSimilarSourceMember_OffersConfigureLineOnly()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<OrderSource, OrderTarget>());
        var trackingSection = message.Substring(message.IndexOf("'OrderTarget.TrackingId'", StringComparison.Ordinal));

        trackingSection.Should().StartWith("'OrderTarget.TrackingId' in map OrderSource -> OrderTarget\n    Configure: ");
    }

    [Fact]
    public void AssertConfigurationIsValid_TwoEquallyCloseTypos_GivesOptionsInsteadOfFix()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<TwoTypos, OneMember>());

        message.Should().NotContain("Fix:");
        message.Should().Contain("Options: s.Sendar (String), s.Sendor (String)");
    }

    [Fact]
    public void AssertConfigurationIsValid_MoreThanTenUnmappedMembers_ListsTenAndCountsTheRest()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<Narrow, ManyMembers>());

        message.Should().Contain("'ManyMembers.M10'");
        message.Should().NotContain("'ManyMembers.M11'");
        message.Should().Contain("+2 more in this map");
    }

    [Fact]
    public void AssertConfigurationIsValid_SameShortNameInTwoMaps_QualifiesTheHeaders()
    {
        var message = ValidationMessage(cfg =>
        {
            cfg.CreateMap<Narrow, Sales.Dto>();
            cfg.CreateMap<Narrow, Billing.Dto>();
        });

        message.Should().Contain("EggMapper.UnitTests.ConfigurationErrorMessageTests.Narrow -> EggMapper.UnitTests.ConfigurationErrorMessageTests.Sales.Dto");
        message.Should().Contain("EggMapper.UnitTests.ConfigurationErrorMessageTests.Narrow -> EggMapper.UnitTests.ConfigurationErrorMessageTests.Billing.Dto");
    }

    [Fact]
    public void AssertConfigurationIsValid_MapCreatedByReverseMap_SaysWhereItCameFrom()
    {
        var message = ValidationMessage(cfg => cfg.CreateMap<Wide, Narrow>().ReverseMap());

        message.Should().Contain("Created by .ReverseMap() on CreateMap<Wide, Narrow>(), which maps by name only. Configure this direction with its own CreateMap<Narrow, Wide>().");
        message.Should().NotContain("was ignored");
    }

    [Fact]
    public void AssertConfigurationIsValid_ReverseMapBlocksExplicitMapFromLaterProfile_SaysTheExplicitMapWasIgnored()
    {
        var message = ValidationMessage(cfg =>
        {
            cfg.AddProfile<ForwardProfile>();
            cfg.AddProfile<ExplicitReverseProfile>();
        });

        message.Should().Contain("A CreateMap<Narrow, Wide>() in a profile was ignored because this ReverseMap() registered the pair first; remove .ReverseMap() from CreateMap<Wide, Narrow>() to use it.");
    }

    [Fact]
    public void AssertConfigurationIsValid_ExplicitCreateMapAfterReverseMap_ReplacesTheReverseMap()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Wide, Narrow>().ReverseMap();
            cfg.CreateMap<Narrow, Wide>().ForMember(d => d.Extra, o => o.Ignore());
        });

        var act = () => config.AssertConfigurationIsValid();

        act.Should().NotThrow();
    }

    [Fact]
    public void Map_MissingPairWithRegisteredReverse_SuggestsReverseMap()
    {
        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<Wide, Narrow>()).CreateMapper();

        var act = () => mapper.Map<Narrow, Wide>(new Narrow());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No mapping configured for Narrow -> Wide. Call CreateMap<Narrow, Wide>() in your mapper configuration, or add .ReverseMap() to the existing CreateMap<Wide, Narrow>().");
    }

    [Fact]
    public void Map_MissingPairWithoutReverse_SuggestsCreateMapOnly()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();

        var act = () => mapper.Map<Narrow, Wide>(new Narrow());

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("No mapping configured for Narrow -> Wide. Call CreateMap<Narrow, Wide>() in your mapper configuration.");
    }

    [Fact]
    public void MapList_MissingElementMap_SuggestsCreateMap()
    {
        var mapper = new MapperConfiguration(_ => { }).CreateMapper();

        var act = () => mapper.MapList<Narrow, Wide>(new List<Narrow> { new() });

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*Call CreateMap<Narrow, Wide>()*");
    }
}

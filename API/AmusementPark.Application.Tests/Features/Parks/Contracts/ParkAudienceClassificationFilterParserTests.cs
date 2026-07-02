using AmusementPark.Application.Features.Parks.Contracts;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Contracts;

public sealed class ParkAudienceClassificationFilterParserTests
{
    [Theory]
    [InlineData("international", ParkAudienceClassificationFilter.International)]
    [InlineData(" National ", ParkAudienceClassificationFilter.National)]
    [InlineData("régional", ParkAudienceClassificationFilter.Regional)]
    [InlineData("regional", ParkAudienceClassificationFilter.Regional)]
    [InlineData("local", ParkAudienceClassificationFilter.Local)]
    [InlineData("Unspecified", ParkAudienceClassificationFilter.Unspecified)]
    [InlineData("non-renseigné", ParkAudienceClassificationFilter.Unspecified)]
    [InlineData("not_specified", ParkAudienceClassificationFilter.Unspecified)]
    [InlineData("missing", ParkAudienceClassificationFilter.Unspecified)]
    public void Parse_WhenAliasIsKnown_ShouldReturnExpectedFilter(string value, ParkAudienceClassificationFilter expected)
    {
        ParkAudienceClassificationFilter? result = ParkAudienceClassificationFilterParser.Parse(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("unknown")]
    public void Parse_WhenAliasIsUnknownOrBlank_ShouldReturnNull(string? value)
    {
        ParkAudienceClassificationFilter? result = ParkAudienceClassificationFilterParser.Parse(value);

        Assert.Null(result);
    }
}

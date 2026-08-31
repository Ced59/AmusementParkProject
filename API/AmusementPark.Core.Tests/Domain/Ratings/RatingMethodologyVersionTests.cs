using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class RatingMethodologyVersionTests
{
    [Fact]
    public void Parse_WhenValueIsValid_ShouldNormalizeAndPreserveIt()
    {
        RatingMethodologyVersion version = RatingMethodologyVersion.Parse("  ratings-2026-01  ");

        Assert.Equal("ratings-2026-01", version.Value);
        Assert.Equal(version.Value, version.ToString());
    }

    [Fact]
    public void Parse_WhenValueIsMissing_ShouldExposeTheStableIdentifierError()
    {
        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(
            () => RatingMethodologyVersion.Parse(" "));

        Assert.Equal(IdentifierErrorCodes.Required, exception.ErrorCode);
    }

    [Fact]
    public void Value_WhenVersionIsUninitialized_ShouldRejectTheDefaultStruct()
    {
        RatingMethodologyVersion version = default;

        Assert.Throws<InvalidOperationException>(() => version.Value);
    }
}

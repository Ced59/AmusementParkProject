using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class SharePublicationIdTests
{
    [Fact]
    public void Parse_ShouldNormalizeAndPreserveAnOpaqueIdentifier()
    {
        SharePublicationId publicationId = SharePublicationId.Parse("  publication-1  ");

        Assert.Equal("publication-1", publicationId.Value);
        Assert.Equal("publication-1", publicationId.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_WhenValueIsMissing_ShouldReturnFalse(string? value)
    {
        bool parsed = SharePublicationId.TryParse(value, out SharePublicationId publicationId);

        Assert.False(parsed);
        Assert.Equal(default, publicationId);
    }

    [Fact]
    public void Parse_WhenValueContainsAControlCharacter_ShouldRejectIt()
    {
        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(
            () => SharePublicationId.Parse("publication\n1"));

        Assert.Equal(IdentifierErrorCodes.ControlCharacter, exception.ErrorCode);
    }

    [Fact]
    public void New_ShouldCreateDistinctOpaqueValues()
    {
        SharePublicationId first = SharePublicationId.New();
        SharePublicationId second = SharePublicationId.New();

        Assert.NotEqual(first, second);
        Assert.Equal(32, first.Value.Length);
        Assert.Equal(32, second.Value.Length);
    }
}

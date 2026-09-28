using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.Identifiers;
using Xunit;

namespace AmusementPark.Application.Tests.Features.LiveData.Models;

public sealed class LiveProviderReadRequestTests
{
    [Fact]
    public void Constructor_WhenIdentifierUsesSharedMaximumLength_ShouldAcceptValue()
    {
        string identifier = new string('a', IdentifierRules.MaximumLength);

        LiveProviderReadRequest request = new LiveProviderReadRequest(identifier);

        Assert.Equal(identifier, request.ExternalEntityId);
    }

    [Fact]
    public void Constructor_WhenIdentifierExceedsSharedMaximumLength_ShouldRejectValue()
    {
        string identifier = new string('a', IdentifierRules.MaximumLength + 1);

        IdentifierValidationException exception = Assert.Throws<IdentifierValidationException>(() =>
            new LiveProviderReadRequest(identifier));

        Assert.Equal(IdentifierErrorCodes.TooLong, exception.ErrorCode);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void Constructor_WhenIdentifierIsRelativePathSegment_ShouldRejectValue(string identifier)
    {
        Assert.Throws<ArgumentException>(() => new LiveProviderReadRequest(identifier));
    }
}

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

    [Theory]
    [InlineData("\"strong-v1\"")]
    [InlineData("W/\"weak-v1\"")]
    public void Constructor_WhenEntityTagIsConcrete_ShouldAcceptValue(string entityTag)
    {
        LiveProviderReadRequest request = new LiveProviderReadRequest("park-root", entityTag);

        Assert.Equal(entityTag, request.EntityTag);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("weak-v1")]
    [InlineData("w/\"lowercase-weak-prefix\"")]
    [InlineData("\"embedded\"quote\"")]
    public void Constructor_WhenEntityTagIsNotConcrete_ShouldRejectValue(string entityTag)
    {
        Assert.Throws<ArgumentException>(() =>
            new LiveProviderReadRequest("park-root", entityTag));
    }
}

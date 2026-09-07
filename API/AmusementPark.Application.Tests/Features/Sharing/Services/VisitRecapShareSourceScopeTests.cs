using AmusementPark.Application.Features.Sharing.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class VisitRecapShareSourceScopeTests
{
    [Theory]
    [InlineData("owner:with:separators", "visit/with spaces")]
    [InlineData("utilisateur-é", "visite-été")]
    public void CreateAndParse_ShouldRoundTripWithoutDelimiterAmbiguity(string ownerUserId, string visitId)
    {
        string scope = VisitRecapShareSourceScope.Create(ownerUserId, visitId);

        bool parsed = VisitRecapShareSourceScope.TryParse(
            scope,
            out string parsedOwner,
            out string parsedVisit);

        Assert.True(parsed);
        Assert.Equal(ownerUserId, parsedOwner);
        Assert.Equal(visitId, parsedVisit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("visit-recap")]
    [InlineData("visit-recap:%%%:%%%")]
    public void TryParse_WhenScopeIsInvalid_ShouldFailWithoutThrowing(string scope)
    {
        bool parsed = VisitRecapShareSourceScope.TryParse(scope, out _, out _);

        Assert.False(parsed);
    }
}

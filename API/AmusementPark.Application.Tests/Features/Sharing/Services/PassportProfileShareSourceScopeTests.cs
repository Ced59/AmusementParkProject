using AmusementPark.Application.Features.Sharing.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PassportProfileShareSourceScopeTests
{
    [Fact]
    public void CreateAndTryParse_ShouldKeepTheOwnerOpaqueAndReversibleInternally()
    {
        const string ownerUserId = "owner:with/slashes";

        string scope = PassportProfileShareSourceScope.Create(ownerUserId);
        bool parsed = PassportProfileShareSourceScope.TryParse(scope, out string restoredOwnerUserId);

        Assert.True(parsed);
        Assert.Equal(ownerUserId, restoredOwnerUserId);
        Assert.DoesNotContain(ownerUserId, scope, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("passport-profile:not-base64!!")]
    [InlineData("visit:abc")]
    public void TryParse_WhenScopeIsInvalid_ShouldFail(string? value)
    {
        Assert.False(PassportProfileShareSourceScope.TryParse(value, out string ownerUserId));
        Assert.Empty(ownerUserId);
    }
}

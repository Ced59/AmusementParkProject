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

    [Fact]
    public void CreateFingerprint_ShouldNormalizeSelectionWithoutMixingYearsAndParkIds()
    {
        string normalized = PassportProfileShareSourceScope.CreateFingerprint(
            "owner-1",
            new[] { 2026, 2026 },
            new[] { " park-1 ", "park-1" });
        string reordered = PassportProfileShareSourceScope.CreateFingerprint(
            "owner-1",
            new[] { 2026 },
            new[] { "park-1" });
        string ambiguousWithoutTypeBoundaries = PassportProfileShareSourceScope.CreateFingerprint(
            "owner-1",
            new[] { 2026, 2027 },
            new[] { "park-1" });
        string numericParkId = PassportProfileShareSourceScope.CreateFingerprint(
            "owner-1",
            new[] { 2026 },
            new[] { "2027", "park-1" });

        Assert.Equal(normalized, reordered);
        Assert.NotEqual(ambiguousWithoutTypeBoundaries, numericParkId);
        Assert.DoesNotContain("owner-1", normalized, StringComparison.Ordinal);
    }
}

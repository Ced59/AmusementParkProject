using AmusementPark.Application.Features.Sharing.Services;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class YearRecapShareSourceScopeTests
{
    [Fact]
    public void CreateAndTryParse_ShouldRoundTripOwnerAndYearWithoutExposingTheOwner()
    {
        string scope = YearRecapShareSourceScope.Create("owner:with-é", 2026);

        bool parsed = YearRecapShareSourceScope.TryParse(
            scope,
            out string ownerUserId,
            out int year);

        Assert.True(parsed);
        Assert.Equal("owner:with-é", ownerUserId);
        Assert.Equal(2026, year);
        Assert.DoesNotContain("owner:with-é", scope, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("year-recap:not-base64:2026")]
    [InlineData("year-recap:b3duZXItMQ:0")]
    [InlineData("visit-recap:b3duZXItMQ:2026")]
    public void TryParse_WhenScopeIsMalformed_ShouldRejectIt(string? scope)
    {
        Assert.False(YearRecapShareSourceScope.TryParse(scope, out _, out _));
    }
}

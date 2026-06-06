using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Users;

public sealed class RefreshTokenTests
{
    [Fact]
    public void IsActiveAt_WhenTokenHasHashFutureExpiryAndNoRevocation_ShouldReturnTrue()
    {
        DateTime now = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);
        RefreshToken token = new RefreshToken
        {
            TokenHash = "hash",
            ExpiresAtUtc = now.AddMinutes(1),
            RevokedAtUtc = null,
        };

        Boolean result = token.IsActiveAt(now);

        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsActiveAt_WhenTokenHashIsMissing_ShouldReturnFalse(string? tokenHash)
    {
        DateTime now = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);
        RefreshToken token = new RefreshToken
        {
            TokenHash = tokenHash!,
            ExpiresAtUtc = now.AddMinutes(1),
        };

        Boolean result = token.IsActiveAt(now);

        Assert.False(result);
    }

    [Fact]
    public void IsActiveAt_WhenTokenIsRevoked_ShouldReturnFalse()
    {
        DateTime now = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);
        RefreshToken token = new RefreshToken
        {
            TokenHash = "hash",
            ExpiresAtUtc = now.AddMinutes(1),
            RevokedAtUtc = now.AddSeconds(-1),
        };

        Boolean result = token.IsActiveAt(now);

        Assert.False(result);
    }

    [Fact]
    public void IsActiveAt_WhenTokenExpiresExactlyNow_ShouldReturnFalse()
    {
        DateTime now = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);
        RefreshToken token = new RefreshToken
        {
            TokenHash = "hash",
            ExpiresAtUtc = now,
        };

        Boolean result = token.IsActiveAt(now);

        Assert.False(result);
    }

    [Fact]
    public void IsActiveAt_WhenTokenExpiredBeforeNow_ShouldReturnFalse()
    {
        DateTime now = new DateTime(2026, 6, 6, 10, 0, 0, DateTimeKind.Utc);
        RefreshToken token = new RefreshToken
        {
            TokenHash = "hash",
            ExpiresAtUtc = now.AddTicks(-1),
        };

        Boolean result = token.IsActiveAt(now);

        Assert.False(result);
    }
}

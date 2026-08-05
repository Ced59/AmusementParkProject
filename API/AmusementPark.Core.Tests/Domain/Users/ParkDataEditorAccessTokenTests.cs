using AmusementPark.Core.Domain.Users;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Users;

public sealed class ParkDataEditorAccessTokenTests
{
    [Fact]
    public void IsActiveAt_ShouldRequireHashFutureExpiryAndNoRevocation()
    {
        DateTime utcNow = new DateTime(2026, 8, 5, 12, 0, 0, DateTimeKind.Utc);
        ParkDataEditorAccessToken token = new ParkDataEditorAccessToken
        {
            TokenHash = "hash",
            ExpiresAtUtc = utcNow.AddMinutes(1),
        };

        Assert.True(token.IsActiveAt(utcNow));

        token.RevokedAtUtc = utcNow;
        Assert.False(token.IsActiveAt(utcNow));
    }
}

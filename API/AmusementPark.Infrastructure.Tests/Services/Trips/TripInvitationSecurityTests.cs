using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Trips;
using AmusementPark.Infrastructure.Services.Trips;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.Trips;

public sealed class TripInvitationSecurityTests
{
    [Fact]
    public void CreateToken_ShouldGenerateA256BitCanonicalTokenAndStoreOnlyItsHash()
    {
        TripInvitationSecurity security = CreateSecurity();

        TripInvitationTokenMaterial token = security.CreateToken(
            TripInvitationId.Parse("invitation-1"),
            "user-1",
            "operation-hash",
            "request-hash");

        Assert.Equal(43, token.PlainTextToken.Length);
        Assert.DoesNotContain('=', token.PlainTextToken);
        Assert.DoesNotContain(token.PlainTextToken, token.TokenHash, StringComparison.Ordinal);
        Assert.True(security.TryHashPublicToken(token.PlainTextToken, out string computedHash));
        Assert.Equal(token.TokenHash, computedHash);
        Assert.False(security.TryHashPublicToken($"{token.PlainTextToken}=", out _));
    }

    [Fact]
    public void TryRevealToken_ShouldAllowAnExactRetryButRejectAnotherActor()
    {
        TripInvitationSecurity security = CreateSecurity();
        TripInvitationId invitationId = TripInvitationId.Parse("invitation-1");
        TripInvitationTokenMaterial token = security.CreateToken(
            invitationId,
            "user-1",
            "operation-hash",
            "request-hash");
        TripInvitation invitation = CreateInvitation(invitationId, token.TokenHash, token.TokenHint);
        TripInvitationCreationRecord record = new(
            invitation,
            "operation-hash",
            "request-hash",
            token.SealedToken,
            token.KeyVersion,
            true);

        Assert.True(security.TryRevealToken(record, "user-1", out string revealed));
        Assert.Equal(token.PlainTextToken, revealed);
        Assert.False(security.TryRevealToken(record, "user-2", out _));
    }

    [Fact]
    public void FingerprintEmail_ShouldBeStableWithoutPersistingTheAddress()
    {
        TripInvitationSecurity security = CreateSecurity();

        TripInvitationEmailFingerprint first = security.FingerprintEmail("guest@example.com");
        TripInvitationEmailFingerprint second = security.FingerprintEmail("guest@example.com");

        Assert.Equal(first, second);
        Assert.DoesNotContain("guest@example.com", first.Hmac, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("v1", first.KeyVersion);
    }

    private static TripInvitationSecurity CreateSecurity()
    {
        TripFingerprintKeyRingSettings settings = new()
        {
            CurrentVersion = "v1",
            CurrentKey = Convert.ToBase64String(Enumerable.Range(1, 32).Select(static value => (byte)value).ToArray()),
        };
        return new TripInvitationSecurity(settings);
    }

    private static TripInvitation CreateInvitation(
        TripInvitationId invitationId,
        string tokenHash,
        string tokenHint)
    {
        DateTime nowUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        return TripInvitation.Create(
            invitationId,
            TripPlanId.Parse("trip-1"),
            "Voyage été",
            tokenHash,
            tokenHint,
            TripDelegatedRole.Viewer,
            TripMemberId.Parse("member-1"),
            "Camille",
            null,
            null,
            TripInvitationPeriodPreview.Unspecified(),
            TripInvitationMemberCountBand.One,
            nowUtc,
            nowUtc.AddDays(7));
    }
}

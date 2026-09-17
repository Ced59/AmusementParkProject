using AmusementPark.Core.Domain.Watchlists;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Watchlists;

public sealed class NotificationEmailPreferenceTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 17, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CreateConsented_ShouldKeepAuditableConsentProof()
    {
        NotificationEmailPreference preference = NotificationEmailPreference.CreateConsented(
            "user-1",
            NotificationEmailPreference.CurrentConsentTextVersion,
            "fr",
            NowUtc);

        Assert.True(preference.IsEnabled);
        Assert.Equal(NotificationEmailPreference.CurrentConsentTextVersion, preference.ConsentTextVersion);
        Assert.Equal("fr", preference.ConsentLocale);
        Assert.Equal(NowUtc, preference.ConsentGrantedAtUtc);
        Assert.Null(preference.RevokedAtUtc);
        Assert.Equal(1, preference.Version);
    }

    [Fact]
    public void Revoke_ShouldKeepConsentProofAndRecordRevocation()
    {
        NotificationEmailPreference preference = NotificationEmailPreference.CreateConsented(
            "user-1",
            NotificationEmailPreference.CurrentConsentTextVersion,
            "fr",
            NowUtc);

        preference.Revoke(NowUtc.AddMinutes(1));

        Assert.False(preference.IsEnabled);
        Assert.Equal(NotificationEmailPreference.CurrentConsentTextVersion, preference.ConsentTextVersion);
        Assert.Equal(NowUtc.AddMinutes(1), preference.RevokedAtUtc);
        Assert.Equal(2, preference.Version);
    }

    [Fact]
    public void RestoreEnabled_WithoutConsentProof_ShouldRejectInvalidState()
    {
        NotificationEmailPreferenceValidationException exception = Assert.Throws<NotificationEmailPreferenceValidationException>(
            () => NotificationEmailPreference.Restore(
                "user-1",
                true,
                null,
                "fr",
                NowUtc,
                null,
                NowUtc,
                NowUtc,
                1));

        Assert.Equal(NotificationEmailPreferenceErrorCodes.InvalidConsent, exception.Code);
    }
}

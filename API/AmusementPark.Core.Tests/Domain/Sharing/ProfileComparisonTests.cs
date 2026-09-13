using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ProfileComparisonTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 15, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Revoke_ByEitherParticipant_ShouldCutResolutionAndAdvanceVersion()
    {
        ProfileComparison comparison = Create();

        comparison.Revoke("acceptor", NowUtc.AddMinutes(1));

        Assert.False(comparison.IsActive);
        Assert.Equal(ProfileComparisonStatus.Revoked, comparison.Status);
        Assert.Equal("acceptor", comparison.RevokedByUserId);
        Assert.Equal(1, comparison.Version);
    }

    [Fact]
    public void Revoke_ByOutsider_ShouldBeRejectedWithoutMutation()
    {
        ProfileComparison comparison = Create();

        ProfileComparisonValidationException exception = Assert.Throws<
            ProfileComparisonValidationException>(() =>
                comparison.Revoke("outsider", NowUtc.AddMinutes(1)));

        Assert.Equal(ProfileComparisonErrorCodes.NotParticipant, exception.ErrorCode);
        Assert.True(comparison.IsActive);
        Assert.Equal(0, comparison.Version);
    }

    private static ProfileComparison Create()
    {
        ProfileComparisonCalculation calculation = new ProfileComparisonCalculation(
            "Camille",
            "Alex",
            new[] { ProfileComparisonCategory.VisitedParks },
            Array.Empty<ProfileComparisonParkResult>(),
            Array.Empty<ProfileComparisonRatingResult>(),
            Array.Empty<ProfileComparisonYearResult>(),
            Array.Empty<ProfileComparisonMissedItemResult>(),
            0,
            ProfileComparisonCalculator.MinimumRatingsForCorrelation,
            null,
            false,
            ProfileComparisonCalculator.CalculationVersion);
        return ProfileComparison.Create(
            ProfileComparisonId.New(),
            ProfileComparisonInvitationId.New(),
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            "creator",
            "acceptor",
            SharePublicationId.Parse("creator-passport"),
            1,
            SharePublicationId.Parse("acceptor-passport"),
            1,
            calculation,
            NowUtc);
    }
}

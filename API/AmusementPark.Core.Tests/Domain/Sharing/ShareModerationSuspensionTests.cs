using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ShareModerationSuspensionTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SharePublication_SuspendAndRestore_ShouldPreservePublicVersionAndToken()
    {
        SharePublication publication = CreatePublishedPublication();
        long publicationVersion = publication.PublicationVersion;
        ShareToken token = publication.ShareToken!.Value;

        publication.SuspendByModeration(NowUtc.AddMinutes(1));

        Assert.False(publication.IsResolvable);
        Assert.True(publication.IsModerationSuspended);
        Assert.Equal(publicationVersion, publication.PublicationVersion);
        Assert.Equal(token, publication.ShareToken);

        publication.RestoreAfterModeration(NowUtc.AddMinutes(2));

        Assert.True(publication.IsResolvable);
        Assert.False(publication.IsModerationSuspended);
        Assert.Equal(publicationVersion, publication.PublicationVersion);
        Assert.Equal(token, publication.ShareToken);
    }

    [Fact]
    public void ProfileComparison_SuspendAndRestore_ShouldOnlyChangePublicResolution()
    {
        ProfileComparison comparison = CreateComparison();

        comparison.SuspendByModeration(NowUtc.AddMinutes(1));

        Assert.True(comparison.IsActive);
        Assert.False(comparison.IsPubliclyResolvable);
        Assert.True(comparison.IsModerationSuspended);

        comparison.RestoreAfterModeration(NowUtc.AddMinutes(2));

        Assert.True(comparison.IsPubliclyResolvable);
        Assert.False(comparison.IsModerationSuspended);
    }

    private static SharePublication CreatePublishedPublication()
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(
            SharePublicationType.VisitRecap);
        SharePublication publication = SharePublication.Create(
            SharePublicationId.New(),
            "owner-1",
            SharePublicationType.VisitRecap,
            "visit-1",
            policy,
            1,
            NowUtc);
        publication.Publish(
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8"),
            ShareVisibility.Unlisted,
            1,
            policy,
            0,
            NowUtc);
        return publication;
    }

    private static ProfileComparison CreateComparison()
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
            ShareToken.Parse("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh4"),
            "creator-1",
            "acceptor-1",
            SharePublicationId.New(),
            1,
            SharePublicationId.New(),
            1,
            calculation,
            NowUtc);
    }
}

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
        ShareModerationReportId reportId = ShareModerationReportId.Parse("report-1");
        long publicationVersion = publication.PublicationVersion;
        ShareToken token = publication.ShareToken!.Value;

        publication.SuspendByModeration(reportId, NowUtc.AddMinutes(1));

        Assert.False(publication.IsResolvable);
        Assert.True(publication.IsModerationSuspended);
        Assert.True(publication.HasModerationSuspension(reportId));
        Assert.Equal(publicationVersion, publication.PublicationVersion);
        Assert.Equal(token, publication.ShareToken);

        publication.RestoreAfterModeration(reportId, NowUtc.AddMinutes(2));

        Assert.True(publication.IsResolvable);
        Assert.False(publication.IsModerationSuspended);
        Assert.Equal(publicationVersion, publication.PublicationVersion);
        Assert.Equal(token, publication.ShareToken);
    }

    [Fact]
    public void ProfileComparison_SuspendAndRestore_ShouldOnlyChangePublicResolution()
    {
        ProfileComparison comparison = CreateComparison();
        ShareModerationReportId reportId = ShareModerationReportId.Parse("report-1");

        comparison.SuspendByModeration(reportId, NowUtc.AddMinutes(1));

        Assert.True(comparison.IsActive);
        Assert.False(comparison.IsPubliclyResolvable);
        Assert.True(comparison.IsModerationSuspended);

        comparison.RestoreAfterModeration(reportId, NowUtc.AddMinutes(2));

        Assert.True(comparison.IsPubliclyResolvable);
        Assert.False(comparison.IsModerationSuspended);
    }

    [Fact]
    public void SharePublication_RestoreFromAnotherReport_ShouldKeepSuspension()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReportId activeReportId =
            ShareModerationReportId.Parse("active-report");
        publication.SuspendByModeration(activeReportId, NowUtc.AddMinutes(1));

        Assert.Throws<SharePublicationValidationException>(() =>
            publication.RestoreAfterModeration(
                ShareModerationReportId.Parse("older-report"),
                NowUtc.AddMinutes(2)));

        Assert.True(publication.HasModerationSuspension(activeReportId));
        Assert.False(publication.IsResolvable);
    }

    [Fact]
    public void SharePublication_MultipleSuspensions_ShouldRestoreOnlyNamedBlock()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReportId firstReportId =
            ShareModerationReportId.Parse("report-1");
        ShareModerationReportId secondReportId =
            ShareModerationReportId.Parse("report-2");
        publication.SuspendByModeration(firstReportId, NowUtc.AddMinutes(1));
        publication.SuspendByModeration(secondReportId, NowUtc.AddMinutes(2));

        publication.RestoreAfterModeration(firstReportId, NowUtc.AddMinutes(3));

        Assert.False(publication.IsResolvable);
        Assert.False(publication.HasModerationSuspension(firstReportId));
        Assert.True(publication.HasModerationSuspension(secondReportId));
    }

    [Fact]
    public void SharePublication_RevokeWhileSuspended_ShouldPreserveSourceBlock()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReportId reportId = ShareModerationReportId.Parse("report-1");
        publication.SuspendByModeration(reportId, NowUtc.AddMinutes(1));

        publication.Revoke(publication.PublicationVersion, NowUtc.AddMinutes(2));

        Assert.Equal(SharePublicationStatus.Revoked, publication.Status);
        Assert.True(publication.HasModerationSuspension(reportId));
        Assert.True(publication.IsModerationSuspended);

        publication.RestoreAfterModeration(reportId, NowUtc.AddMinutes(3));

        Assert.False(publication.IsModerationSuspended);
        Assert.Equal(SharePublicationStatus.Revoked, publication.Status);
    }

    [Fact]
    public void ProfileComparison_RevokeAndRestoreOneReport_ShouldKeepOtherBlock()
    {
        ProfileComparison comparison = CreateComparison();
        ShareModerationReportId firstReportId =
            ShareModerationReportId.Parse("report-1");
        ShareModerationReportId secondReportId =
            ShareModerationReportId.Parse("report-2");
        comparison.SuspendByModeration(firstReportId, NowUtc.AddMinutes(1));
        comparison.SuspendByModeration(secondReportId, NowUtc.AddMinutes(2));

        comparison.Revoke("creator-1", NowUtc.AddMinutes(3));
        comparison.RestoreAfterModeration(firstReportId, NowUtc.AddMinutes(4));

        Assert.Equal(ProfileComparisonStatus.Revoked, comparison.Status);
        Assert.False(comparison.HasModerationSuspension(firstReportId));
        Assert.True(comparison.HasModerationSuspension(secondReportId));
        Assert.False(comparison.IsPubliclyResolvable);
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

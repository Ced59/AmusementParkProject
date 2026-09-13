using AmusementPark.Core.Domain.Sharing;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Sharing;

public sealed class ShareModerationReportTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithOtherReasonWithoutDetails_ShouldReject()
    {
        Assert.Throws<ShareModerationValidationException>(() =>
            ShareModerationReport.Create(
                ShareModerationReportId.New(),
                ShareModerationTargetType.VisitRecap,
                "publication-1",
                ShareModerationReason.Other,
                null,
                NowUtc));
    }

    [Fact]
    public void Create_WithUnsafeDetails_ShouldReject()
    {
        Assert.Throws<ShareModerationValidationException>(() =>
            ShareModerationReport.Create(
                ShareModerationReportId.New(),
                ShareModerationTargetType.PassportProfile,
                "publication-1",
                ShareModerationReason.SpamOrUnsafeLink,
                "https://dangerous.example",
                NowUtc));
    }

    [Fact]
    public void SuspendThenRestore_ShouldKeepACompleteReviewHistory()
    {
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.New(),
            ShareModerationTargetType.ProfileComparison,
            "comparison-1",
            ShareModerationReason.PersonalData,
            "Un nom complet est affiché.",
            NowUtc);

        report.MarkPublicationSuspended("admin-1", "Masqué pendant la vérification.", NowUtc.AddMinutes(1));
        report.MarkPublicationRestored("admin-2", "Le propriétaire a corrigé le nom.", NowUtc.AddMinutes(2));

        Assert.Equal(ShareModerationReportStatus.PublicationRestored, report.Status);
        Assert.Equal("admin-2", report.ReviewedByUserId);
        Assert.Equal(2, report.Version);
    }

    [Fact]
    public void Dismiss_WhenAlreadyReviewed_ShouldReject()
    {
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.New(),
            ShareModerationTargetType.YearRecap,
            "publication-1",
            ShareModerationReason.MisleadingContent,
            null,
            NowUtc);
        report.Dismiss("admin-1", null, NowUtc.AddMinutes(1));

        Assert.Throws<ShareModerationValidationException>(() =>
            report.Dismiss("admin-1", null, NowUtc.AddMinutes(2)));
    }
}

using AmusementPark.Core.Domain.ParkFit;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.ParkFit;

public sealed class ParkFitSourceReportTests
{
    private static readonly DateTime SubmittedAtUtc =
        new DateTime(2026, 9, 14, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizeEvidenceAndRemainPending()
    {
        ParkFitSourceReport report = ParkFitSourceReport.Create(
            ParkFitSourceReportId.New(),
            " park-1 ",
            " Parc témoin ",
            ParkFitEvidenceKind.AccessCondition,
            "https://example.org/access",
            " page 4 ",
            ParkFitSourceReportReason.Outdated,
            " À revérifier ",
            SubmittedAtUtc);

        Assert.Equal("park-1", report.ParkId);
        Assert.Equal("Parc témoin", report.ParkName);
        Assert.Equal("page 4", report.SourceReference);
        Assert.Equal(ParkFitSourceReportStatus.Pending, report.Status);
        Assert.Equal(0, report.Revision);
    }

    [Fact]
    public void Create_WithUnsafeOrNonHttpsSource_ShouldReject()
    {
        Assert.Throws<ArgumentException>(() => ParkFitSourceReport.Create(
            ParkFitSourceReportId.New(),
            "park-1",
            "Parc témoin",
            ParkFitEvidenceKind.OpeningCalendar,
            "http://example.org/calendar",
            null,
            ParkFitSourceReportReason.Incorrect,
            "<script>",
            SubmittedAtUtc));
    }

    [Fact]
    public void Create_WithMarkupInSourceReference_ShouldReject()
    {
        Assert.Throws<ArgumentException>(() => ParkFitSourceReport.Create(
            ParkFitSourceReportId.New(),
            "park-1",
            "Parc témoin",
            ParkFitEvidenceKind.AccessCondition,
            null,
            "<strong>restriction</strong>",
            ParkFitSourceReportReason.Incorrect,
            null,
            SubmittedAtUtc));
    }

    [Fact]
    public void Resolve_ShouldVersionReviewWithoutExposingReviewerInResultContracts()
    {
        ParkFitSourceReport report = ParkFitSourceReport.Create(
            ParkFitSourceReportId.New(),
            "park-1",
            "Parc témoin",
            ParkFitEvidenceKind.GeneralParkData,
            null,
            null,
            ParkFitSourceReportReason.Incomplete,
            null,
            SubmittedAtUtc);

        report.Resolve("admin-1", "Donnée complétée", SubmittedAtUtc.AddHours(1));

        Assert.Equal(ParkFitSourceReportStatus.Resolved, report.Status);
        Assert.Equal(1, report.Revision);
        Assert.Equal("admin-1", report.ReviewedByUserId);
        Assert.Throws<InvalidOperationException>(() =>
            report.Dismiss("admin-1", null, SubmittedAtUtc.AddHours(2)));
    }
}

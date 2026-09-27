using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalExistenceReportTests
{
    private static readonly DateTime SubmittedAtUtc =
        new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldNormalizePrivateVisitEvidenceAndRemainPending()
    {
        HistoricalExistenceReport report = HistoricalExistenceReport.Create(
            HistoricalExistenceReportId.New(),
            " owner-1 ",
            VisitId.Parse("visit-1"),
            " park-1 ",
            " Parc témoin ",
            VisitDate.ForYear(1998, isApproximate: true),
            " Ancien Cyclone ",
            "https://example.org/archive",
            " plan 1998 ",
            " près du lac ",
            SubmittedAtUtc);

        Assert.Equal("owner-1", report.OwnerUserId);
        Assert.Equal("Ancien Cyclone", report.ClaimedName);
        Assert.Equal("ANCIEN CYCLONE", report.NormalizedClaimedName);
        Assert.Equal("plan 1998", report.SourceReference);
        Assert.Equal(HistoricalExistenceReportStatus.Pending, report.Status);
        Assert.Equal(0, report.Revision);
    }

    [Fact]
    public void Create_WithUnsafeEvidence_ShouldReject()
    {
        Assert.Throws<ArgumentException>(() => HistoricalExistenceReport.Create(
            HistoricalExistenceReportId.New(),
            "owner-1",
            VisitId.Parse("visit-1"),
            "park-1",
            "Parc témoin",
            VisitDate.ForDay(1998, 7, 4),
            "Ancien Cyclone",
            "http://example.org/archive",
            null,
            "<script>",
            SubmittedAtUtc));
    }

    [Fact]
    public void AcceptForResearch_ShouldNotCreateACanonicalFactAndCannotBeRepeated()
    {
        HistoricalExistenceReport report = CreateReport();

        report.AcceptForResearch(
            "admin-1",
            "Source à recouper",
            SubmittedAtUtc.AddHours(1));

        Assert.Equal(HistoricalExistenceReportStatus.AcceptedForResearch, report.Status);
        Assert.Equal(1, report.Revision);
        Assert.Equal("admin-1", report.ReviewedByUserId);
        Assert.Throws<InvalidOperationException>(() => report.Dismiss(
            "admin-1",
            null,
            SubmittedAtUtc.AddHours(2)));
    }

    private static HistoricalExistenceReport CreateReport()
    {
        return HistoricalExistenceReport.Create(
            HistoricalExistenceReportId.New(),
            "owner-1",
            VisitId.Parse("visit-1"),
            "park-1",
            "Parc témoin",
            VisitDate.ForYear(1998),
            "Ancien Cyclone",
            null,
            null,
            null,
            SubmittedAtUtc);
    }
}

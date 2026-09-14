using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class ParkFitSourceReportMongoMapperTests
{
    [Fact]
    public void ToDocumentToDomain_ShouldPreserveTheReviewedReport()
    {
        DateTime submittedAtUtc = new DateTime(2026, 9, 14, 8, 0, 0, DateTimeKind.Utc);
        DateTime reviewedAtUtc = submittedAtUtc.AddHours(1);
        ParkFitSourceReport report = ParkFitSourceReport.Create(
            ParkFitSourceReportId.Parse("7d283106-142f-49cf-adfb-2f2728466fa9"),
            "park-1",
            "Parc test",
            ParkFitEvidenceKind.AccessCondition,
            "https://example.com/access",
            "Condition de taille",
            ParkFitSourceReportReason.Incorrect,
            "La taille a changé",
            submittedAtUtc);
        report.Resolve("admin-1", "Source corrigée", reviewedAtUtc);

        ParkFitSourceReport restored = report.ToDocument().ToDomain();

        Assert.Equal(report.Id, restored.Id);
        Assert.Equal(ParkFitSourceReportStatus.Resolved, restored.Status);
        Assert.Equal("admin-1", restored.ReviewedByUserId);
        Assert.Equal("Source corrigée", restored.DecisionNote);
        Assert.Equal(reviewedAtUtc, restored.ReviewedAtUtc);
        Assert.Equal(1, restored.Revision);
    }
}

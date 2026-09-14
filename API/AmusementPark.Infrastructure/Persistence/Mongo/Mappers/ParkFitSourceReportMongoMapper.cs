using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class ParkFitSourceReportMongoMapper
{
    public static ParkFitSourceReportDocument ToDocument(this ParkFitSourceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new ParkFitSourceReportDocument
        {
            Id = report.Id.Value,
            ParkId = report.ParkId,
            ParkName = report.ParkName,
            EvidenceKind = report.EvidenceKind,
            SourceUrl = report.SourceUrl,
            SourceReference = report.SourceReference,
            Reason = report.Reason,
            Details = report.Details,
            Status = report.Status,
            SubmittedAtUtc = report.SubmittedAtUtc,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedAtUtc = report.ReviewedAtUtc,
            DecisionNote = report.DecisionNote,
            Revision = report.Revision,
            CreatedAt = report.SubmittedAtUtc,
            UpdatedAt = report.ReviewedAtUtc ?? report.SubmittedAtUtc,
        };
    }

    public static ParkFitSourceReport ToDomain(this ParkFitSourceReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ParkFitSourceReport.Restore(
            ParkFitSourceReportId.Parse(document.Id),
            document.ParkId,
            document.ParkName,
            document.EvidenceKind,
            document.SourceUrl,
            document.SourceReference,
            document.Reason,
            document.Details,
            document.Status,
            document.SubmittedAtUtc,
            document.ReviewedByUserId,
            document.ReviewedAtUtc,
            document.DecisionNote,
            document.Revision);
    }
}

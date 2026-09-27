using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class HistoricalExistenceReportMongoMapper
{
    public static HistoricalExistenceReportDocument ToDocument(
        this HistoricalExistenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new HistoricalExistenceReportDocument
        {
            Id = report.Id.Value,
            OwnerUserId = report.OwnerUserId,
            VisitId = report.VisitId.Value,
            ParkId = report.ParkId,
            ParkName = report.ParkName,
            VisitDate = ToDocument(report.VisitDate),
            ClaimedName = report.ClaimedName,
            NormalizedClaimedName = report.NormalizedClaimedName,
            SourceUrl = report.SourceUrl,
            SourceReference = report.SourceReference,
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

    public static HistoricalExistenceReport ToDomain(
        this HistoricalExistenceReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return HistoricalExistenceReport.Restore(
            HistoricalExistenceReportId.Parse(document.Id),
            document.OwnerUserId,
            VisitId.Parse(document.VisitId),
            document.ParkId,
            document.ParkName,
            ToDomain(document.VisitDate),
            document.ClaimedName,
            document.SourceUrl,
            document.SourceReference,
            document.Details,
            document.Status,
            document.SubmittedAtUtc,
            document.ReviewedByUserId,
            document.ReviewedAtUtc,
            document.DecisionNote,
            document.Revision);
    }

    private static VisitDateDocument ToDocument(VisitDate date)
    {
        return new VisitDateDocument
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            Precision = date.Precision,
            IsApproximate = date.IsApproximate,
        };
    }

    private static VisitDate ToDomain(VisitDateDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new VisitDate(
            document.Year,
            document.Month,
            document.Day,
            document.Precision,
            document.IsApproximate);
    }
}

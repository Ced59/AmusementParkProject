using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class ShareModerationReportMongoMapper
{
    public static ShareModerationReportDocument ToDocument(this ShareModerationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new ShareModerationReportDocument
        {
            Id = report.Id.Value,
            TargetType = report.TargetType,
            TargetRecordId = report.TargetRecordId,
            Reason = report.Reason,
            Details = report.Details,
            Status = report.Status,
            SubmittedAtUtc = report.SubmittedAtUtc,
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedAtUtc = report.ReviewedAtUtc,
            DecisionNote = report.DecisionNote,
            Version = report.Version,
            CreatedAt = report.SubmittedAtUtc,
            UpdatedAt = report.ReviewedAtUtc ?? report.SubmittedAtUtc,
        };
    }

    public static ShareModerationReport ToDomain(this ShareModerationReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ShareModerationReport.Restore(
            ShareModerationReportId.Parse(document.Id),
            document.TargetType,
            document.TargetRecordId,
            document.Reason,
            document.Details,
            document.Status,
            document.SubmittedAtUtc,
            document.ReviewedByUserId,
            document.ReviewedAtUtc,
            document.DecisionNote,
            document.Version);
    }
}

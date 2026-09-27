using AmusementPark.Application.Features.HistoricalExistenceReports.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.HistoricalExistenceReports;

public static class HistoricalExistenceReportResultMapper
{
    public static HistoricalExistenceReportResult ToResult(
        this HistoricalExistenceReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new HistoricalExistenceReportResult(
            report.Id.Value,
            report.ParkId,
            report.ParkName,
            report.VisitDate,
            report.ClaimedName,
            report.SourceUrl,
            report.SourceReference,
            report.Details,
            report.Status,
            report.SubmittedAtUtc,
            report.ReviewedAtUtc,
            report.DecisionNote,
            report.Revision);
    }
}

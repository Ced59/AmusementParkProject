using System.Globalization;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;

namespace AmusementPark.WebAPI.Mappers;

public static class AdminHistoricalParkDiagnosticsHttpMapper
{
    public static AdminHistoricalParkDiagnosticsDto ToHttp(
        this AdminHistoricalParkDiagnosticsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new AdminHistoricalParkDiagnosticsDto
        {
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            FactCount = result.Diagnostics.FactCount,
            RelationCount = result.Diagnostics.RelationCount,
            BlockingIssueCount = result.Diagnostics.BlockingIssueCount,
            Issues = result.Diagnostics.Issues.Select(ToHttp).ToArray(),
            DecadeCoverage = result.Diagnostics.DecadeCoverage.Select(ToHttp).ToArray(),
            Workflow = result.Diagnostics.Workflow.Select(ToHttp).ToArray(),
            Visits = new AdminHistoricalVisitDiagnosticsDto
            {
                PotentiallyInconsistentVisitCount =
                    result.VisitCounts.PotentiallyInconsistentVisitCount,
                ConfirmedConflictVisitCount = result.VisitCounts.ConfirmedConflictVisitCount,
                UnverifiedVisitCount = result.VisitCounts.UnverifiedVisitCount,
            },
            RolloutGate = new AdminHistoricalParkRolloutGateDto
            {
                IsOpen = result.RolloutGate.IsOpen,
                HasEnoughStructuredFacts = result.RolloutGate.HasEnoughStructuredFacts,
                HasCompleteSourceCoverage = result.RolloutGate.HasCompleteSourceCoverage,
                HasMajorMilestone = result.RolloutGate.HasMajorMilestone,
                HasIndexableKeyYear = result.RolloutGate.HasIndexableKeyYear,
                PublishedFactCount = result.RolloutGate.PublishedFactCount,
                SourcedFactCount = result.RolloutGate.SourcedFactCount,
                MajorFactCount = result.RolloutGate.MajorFactCount,
                IndexableKeyYears = result.RolloutGate.IndexableKeyYears,
            },
        };
    }

    private static AdminHistoricalDiagnosticIssueDto ToHttp(HistoricalParkDiagnosticIssue issue)
    {
        return new AdminHistoricalDiagnosticIssueDto
        {
            Code = issue.Code.ToString(),
            Severity = issue.Severity.ToString(),
            SubjectType = issue.Subject?.Type.ToString(),
            SubjectId = issue.Subject?.Id,
            SubjectLabel = issue.Subject?.HistoricalLabel,
            FactId = issue.FactId?.ToString("D", CultureInfo.InvariantCulture),
            RelationId = issue.RelationId?.ToString("D", CultureInfo.InvariantCulture),
        };
    }

    private static AdminHistoricalDecadeCoverageDto ToHttp(HistoricalDecadeCoverage coverage)
    {
        return new AdminHistoricalDecadeCoverageDto
        {
            Decade = coverage.Decade,
            ResourceCount = coverage.ResourceCount,
            SourcedResourceCount = coverage.SourcedResourceCount,
            PublishedResourceCount = coverage.PublishedResourceCount,
            SubjectCount = coverage.SubjectCount,
            SourceCoveragePercentage = coverage.SourceCoveragePercentage,
        };
    }

    private static AdminHistoricalWorkflowStageDto ToHttp(HistoricalWorkflowStageCount stage)
    {
        return new AdminHistoricalWorkflowStageDto
        {
            Stage = stage.Stage.ToString(),
            ResourceCount = stage.ResourceCount,
        };
    }
}

using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record HistoricalPublicationImpactPreviewResult(
    HistoricalReviewResourceType ResourceType,
    Guid ResourceId,
    string ResourceLabel,
    int PreviewYear,
    bool CanPublish,
    IReadOnlyCollection<string> BlockingReasons,
    int? AffectedFromYear,
    int? AffectedToYear,
    int? AffectedSnapshotYearCount,
    int ChangedSubjectCount,
    HistoricalSnapshotImpactSummary Before,
    HistoricalSnapshotImpactSummary After,
    HistoricalVisitDiagnosticCounts VisitCounts);

using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record VisitHistoricalRideTargetPageResult(
    IReadOnlyCollection<VisitRideTargetEvaluationResult> Items,
    int CurrentPage,
    int PageSize,
    int TotalItems,
    int TotalPages,
    int KnownOpenCount,
    int PossiblyOpenCount,
    int AllHistoryCount,
    HistoricalCoverageStatus CoverageStatus,
    int CoveragePercent,
    string MethodologyVersion);

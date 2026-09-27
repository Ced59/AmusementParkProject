using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Results;

public sealed record PublicHistoricalLineageResult(
    HistoricalSubject Root,
    PublicHistoricalLineageContextParkResult? ContextPark,
    IReadOnlyCollection<HistoricalSubject> Subjects,
    IReadOnlyCollection<PublicHistoricalRelationResult> Relations,
    bool HasDirectedCycle,
    bool IsTruncated,
    int MaximumDepth);

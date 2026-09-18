namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripProgramCoherenceResult(
    string TripPlanId,
    string TripTitle,
    long PlanVersion,
    DateTime EvaluatedAtUtc,
    int CriticalCount,
    int AttentionCount,
    int InformationCount,
    IReadOnlyCollection<TripProgramDayEvidenceResult> Days,
    IReadOnlyCollection<TripProgramTravelSegmentResult> TravelSegments,
    IReadOnlyCollection<TripProgramCoherenceIssueResult> Issues);

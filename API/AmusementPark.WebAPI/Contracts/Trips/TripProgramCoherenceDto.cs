namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripProgramCoherenceDto
{
    public string TripPlanId { get; init; } = string.Empty;

    public string TripTitle { get; init; } = string.Empty;

    public long PlanVersion { get; init; }

    public DateTime EvaluatedAtUtc { get; init; }

    public int CriticalCount { get; init; }

    public int AttentionCount { get; init; }

    public int InformationCount { get; init; }

    public IReadOnlyCollection<TripProgramDayEvidenceDto> Days { get; init; } =
        Array.Empty<TripProgramDayEvidenceDto>();

    public IReadOnlyCollection<TripProgramTravelSegmentDto> TravelSegments { get; init; } =
        Array.Empty<TripProgramTravelSegmentDto>();

    public IReadOnlyCollection<TripProgramCoherenceIssueDto> Issues { get; init; } =
        Array.Empty<TripProgramCoherenceIssueDto>();
}

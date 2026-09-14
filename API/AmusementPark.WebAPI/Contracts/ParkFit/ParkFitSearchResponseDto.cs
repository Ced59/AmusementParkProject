namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSearchResponseDto
{
    public string MethodVersion { get; init; } = string.Empty;

    public DateOnly EvaluationDate { get; init; }

    public DateTime EvaluatedAtUtc { get; init; }

    public long TotalCandidateCount { get; init; }

    public int InspectedCandidateCount { get; init; }

    public int QualityEligibleCandidateCount { get; init; }

    public int QualityRejectedCandidateCount { get; init; }

    public int OperationallySuspendedCandidateCount { get; init; }

    public int NotActivatedCandidateCount { get; init; }

    public bool CandidatePoolTruncated { get; init; }

    public IReadOnlyDictionary<string, int> QualityStatusCounts { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyDictionary<string, int> QualityIssueCounts { get; init; } =
        new Dictionary<string, int>();

    public IReadOnlyCollection<ParkFitSearchParkDto> Parks { get; init; } =
        Array.Empty<ParkFitSearchParkDto>();
}

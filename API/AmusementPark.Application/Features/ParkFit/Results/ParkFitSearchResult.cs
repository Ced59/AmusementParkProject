using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFit.Results;

/// <summary>
/// Résultat global d'une recherche FIT anonyme et bornée.
/// </summary>
public sealed class ParkFitSearchResult
{
    public string MethodVersion { get; init; } = string.Empty;

    public DateOnly EvaluationDate { get; init; }

    public DateTime EvaluatedAtUtc { get; init; }

    public long TotalCandidateCount { get; init; }

    public int InspectedCandidateCount { get; init; }

    public int QualityEligibleCandidateCount { get; init; }

    public int QualityRejectedCandidateCount { get; init; }

    public bool CandidatePoolTruncated { get; init; }

    public IReadOnlyDictionary<ParkFitDataQualityStatus, int> QualityStatusCounts { get; init; } =
        new Dictionary<ParkFitDataQualityStatus, int>();

    public IReadOnlyDictionary<ParkFitDataQualityIssue, int> QualityIssueCounts { get; init; } =
        new Dictionary<ParkFitDataQualityIssue, int>();

    public IReadOnlyCollection<ParkFitSearchParkResult> Parks { get; init; } =
        Array.Empty<ParkFitSearchParkResult>();
}

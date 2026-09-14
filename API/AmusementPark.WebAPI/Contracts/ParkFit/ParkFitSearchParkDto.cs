namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSearchParkDto
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public string? CountryCode { get; init; }

    public string? ParkType { get; init; }

    public string ScoreState { get; init; } = string.Empty;

    public decimal? ComparativeScore { get; init; }

    public decimal? RawKnownScore { get; init; }

    public decimal CoveragePercent { get; init; }

    public decimal KnownWeightPercent { get; init; }

    public decimal? ScoreCeilingPercent { get; init; }

    public string Confidence { get; init; } = string.Empty;

    public string DateAvailabilityState { get; init; } = string.Empty;

    public int UnknownCount { get; init; }

    public int EveryoneTogetherAttractionCount { get; init; }

    public int SplitRequiredAttractionCount { get; init; }

    public int PartialAttractionCount { get; init; }

    public int NoCompatibleMemberAttractionCount { get; init; }

    public int UnknownAttractionCount { get; init; }

    public string DataQualityStatus { get; init; } = string.Empty;

    public int DataQualityCoveragePercent { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public IReadOnlyCollection<string> Reasons { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<ParkFitScoreComponentDto> Components { get; init; } =
        Array.Empty<ParkFitScoreComponentDto>();

    public IReadOnlyCollection<ParkFitSearchMemberSummaryDto> MemberSummaries { get; init; } =
        Array.Empty<ParkFitSearchMemberSummaryDto>();

    public IReadOnlyCollection<ParkFitCriticalSourceDto> CriticalSources { get; init; } =
        Array.Empty<ParkFitCriticalSourceDto>();
}

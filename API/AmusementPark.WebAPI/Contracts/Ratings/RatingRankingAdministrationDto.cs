namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingAdministrationDto
{
    public DateTime GeneratedAtUtc { get; set; }

    public RatingMethodologyDto CurrentMethodology { get; set; } = new RatingMethodologyDto();

    public RatingMethodologyDto? PreparingMethodology { get; set; }

    public RatingDiagnosticsDto DataDiagnostics { get; set; } = new RatingDiagnosticsDto();

    public IReadOnlyCollection<RatingRankingScopeDiagnosticsDto> Scopes { get; set; } =
        Array.Empty<RatingRankingScopeDiagnosticsDto>();

    public IReadOnlyCollection<RatingRankingEvidenceDistributionDto> EvidenceDistribution { get; set; } =
        Array.Empty<RatingRankingEvidenceDistributionDto>();

    public IReadOnlyCollection<RatingRankingNearThresholdTargetDto> NearThresholdTargets { get; set; } =
        Array.Empty<RatingRankingNearThresholdTargetDto>();

    public IReadOnlyCollection<RatingRankingExclusionDistributionDto> Exclusions { get; set; } =
        Array.Empty<RatingRankingExclusionDistributionDto>();

    public IReadOnlyCollection<RatingRankingCategoryCoverageDto> CategoryCoverage { get; set; } =
        Array.Empty<RatingRankingCategoryCoverageDto>();
}

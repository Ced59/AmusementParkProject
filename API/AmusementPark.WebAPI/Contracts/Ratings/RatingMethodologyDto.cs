namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingMethodologyDto
{
    public string Version { get; set; } = string.Empty;

    public DateOnly EffectiveDate { get; set; }

    public bool IsCurrent { get; set; }

    public string? PreviousVersion { get; set; }

    public RatingScaleDto RatingScale { get; set; } = new RatingScaleDto();

    public BayesianRatingParametersDto Bayesian { get; set; } = new BayesianRatingParametersDto();

    public ParkRatingCompositionDto ParkComposition { get; set; } = new ParkRatingCompositionDto();

    public RatingEvidenceThresholdsDto EvidenceThresholds { get; set; } = new RatingEvidenceThresholdsDto();

    public RatingRankingPublicationRulesDto PublicationRules { get; set; } = new RatingRankingPublicationRulesDto();
}

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

public sealed class RatingScaleDto
{
    public decimal Minimum { get; set; }

    public decimal Maximum { get; set; }

    public decimal Step { get; set; }
}

public sealed class BayesianRatingParametersDto
{
    public double PriorMean { get; set; }

    public int PriorWeight { get; set; }
}

public sealed class ParkRatingCompositionDto
{
    public double DirectRatingWeight { get; set; }

    public double ItemRatingWeight { get; set; }

    public bool BalancesItemCategoriesEqually { get; set; }

    public int MinimumEligibleItems { get; set; }

    public int MinimumItemsPerCategory { get; set; }

    public int MinimumCategories { get; set; }
}

public sealed class RatingEvidenceThresholdsDto
{
    public int Provisional { get; set; }

    public int Eligible { get; set; }

    public int Established { get; set; }

    public int Strong { get; set; }
}

public sealed class RatingRankingPublicationRulesDto
{
    public int MinimumEligibleEntries { get; set; }

    public decimal ScoreTieEpsilon { get; set; }

    public string RankingConvention { get; set; } = string.Empty;
}

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Description immuable des paramètres publics d'une méthodologie de classement.
/// </summary>
public sealed record RatingMethodologyDefinition(
    RankingEligibilityPolicy EligibilityPolicy,
    DateOnly EffectiveDate,
    RatingMethodologyVersion? PreviousVersion)
{
    public RatingMethodologyVersion Version => this.EligibilityPolicy.Version;

    public decimal RatingMinimum => RatingValue.MinimumHalfSteps / 2m;

    public decimal RatingMaximum => RatingValue.MaximumHalfSteps / 2m;

    public decimal RatingStep => 0.5m;

    public double BayesianPriorMean => RatingScoreCalculator.PriorMean;

    public int BayesianPriorWeight => RatingScoreCalculator.PriorWeight;

    public double ParkDirectScoreWeight => RatingScoreCalculator.ParkDirectScoreWeight;

    public double ParkItemsScoreWeight => RatingScoreCalculator.ParkItemsScoreWeight;

    public bool BalancesItemCategoriesEqually => true;

    public string RankingConvention => "competition";
}

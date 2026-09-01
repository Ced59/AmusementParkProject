namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Description immuable des paramètres publics d'une méthodologie de classement.
/// </summary>
public sealed record RatingMethodologyDefinition(
    RankingEligibilityPolicy EligibilityPolicy,
    DateOnly EffectiveDate,
    RatingMethodologyVersion? PreviousVersion,
    decimal RatingMinimum,
    decimal RatingMaximum,
    decimal RatingStep,
    double BayesianPriorMean,
    int BayesianPriorWeight,
    double ParkDirectScoreWeight,
    double ParkItemsScoreWeight,
    bool BalancesItemCategoriesEqually,
    string RankingConvention)
{
    public RatingMethodologyVersion Version => this.EligibilityPolicy.Version;
}

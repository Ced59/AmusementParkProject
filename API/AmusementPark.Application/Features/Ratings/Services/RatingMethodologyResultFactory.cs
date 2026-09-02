using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

internal static class RatingMethodologyResultFactory
{
    public static RatingMethodologyResult Create(RatingMethodologyDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        RankingEligibilityPolicy policy = definition.EligibilityPolicy;
        return new RatingMethodologyResult(
            definition.Version,
            definition.EffectiveDate,
            definition.Version == RatingMethodologyCatalog.Current.Version,
            definition.PreviousVersion,
            definition.RatingMinimum,
            definition.RatingMaximum,
            definition.RatingStep,
            definition.BayesianPriorMean,
            definition.BayesianPriorWeight,
            definition.ParkDirectScoreWeight,
            definition.ParkItemsScoreWeight,
            definition.BalancesItemCategoriesEqually,
            policy.ProvisionalMinUniqueContributors,
            policy.EligibleMinUniqueContributors,
            policy.EstablishedMinUniqueContributors,
            policy.StrongEvidenceMinUniqueContributors,
            policy.MinimumEligibleEntriesPerRanking,
            policy.MinimumEligibleItemsForParkItemComponent,
            policy.MinimumEligibleItemsPerCategory,
            policy.MinimumEligibleCategories,
            policy.ScoreTieEpsilon,
            definition.RankingConvention);
    }
}

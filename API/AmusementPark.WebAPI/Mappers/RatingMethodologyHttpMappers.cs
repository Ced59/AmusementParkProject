using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.WebAPI.Contracts.Ratings;

namespace AmusementPark.WebAPI.Mappers;

internal static class RatingMethodologyHttpMappers
{
    public static RatingMethodologyDto ToHttp(this RatingMethodologyResult value)
    {
        return new RatingMethodologyDto
        {
            Version = value.Version.ToString(),
            EffectiveDate = value.EffectiveDate,
            IsCurrent = value.IsCurrent,
            PreviousVersion = value.PreviousVersion?.ToString(),
            RatingScale = new RatingScaleDto
            {
                Minimum = value.RatingMinimum,
                Maximum = value.RatingMaximum,
                Step = value.RatingStep,
            },
            Bayesian = new BayesianRatingParametersDto
            {
                PriorMean = value.BayesianPriorMean,
                PriorWeight = value.BayesianPriorWeight,
            },
            ParkComposition = new ParkRatingCompositionDto
            {
                DirectRatingWeight = value.ParkDirectScoreWeight,
                ItemRatingWeight = value.ParkItemsScoreWeight,
                BalancesItemCategoriesEqually = value.BalancesItemCategoriesEqually,
                MinimumEligibleItems = value.MinimumEligibleItemsForParkItemComponent,
                MinimumItemsPerCategory = value.MinimumEligibleItemsPerCategory,
                MinimumCategories = value.MinimumEligibleCategories,
            },
            EvidenceThresholds = new RatingEvidenceThresholdsDto
            {
                Provisional = value.ProvisionalMinUniqueContributors,
                Eligible = value.EligibleMinUniqueContributors,
                Established = value.EstablishedMinUniqueContributors,
                Strong = value.StrongEvidenceMinUniqueContributors,
            },
            PublicationRules = new RatingRankingPublicationRulesDto
            {
                MinimumEligibleEntries = value.MinimumEligibleEntriesPerRanking,
                ScoreTieEpsilon = value.ScoreTieEpsilon,
                RankingConvention = value.RankingConvention,
            },
        };
    }
}

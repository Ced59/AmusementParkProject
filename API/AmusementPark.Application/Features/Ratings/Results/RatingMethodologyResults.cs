using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingMethodologyResult(
    RatingMethodologyVersion Version,
    DateOnly EffectiveDate,
    bool IsCurrent,
    RatingMethodologyVersion? PreviousVersion,
    decimal RatingMinimum,
    decimal RatingMaximum,
    decimal RatingStep,
    double BayesianPriorMean,
    int BayesianPriorWeight,
    double ParkDirectScoreWeight,
    double ParkItemsScoreWeight,
    bool BalancesItemCategoriesEqually,
    int ProvisionalMinUniqueContributors,
    int EligibleMinUniqueContributors,
    int EstablishedMinUniqueContributors,
    int StrongEvidenceMinUniqueContributors,
    int MinimumEligibleEntriesPerRanking,
    int MinimumEligibleItemsForParkItemComponent,
    int MinimumEligibleItemsPerCategory,
    int MinimumEligibleCategories,
    decimal ScoreTieEpsilon,
    string RankingConvention);

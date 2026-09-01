using System.Diagnostics.CodeAnalysis;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Catalogue métier des méthodologies publiées, de la plus récente à la plus ancienne.
/// </summary>
public static class RatingMethodologyCatalog
{
    private static readonly RatingMethodologyDefinition Initial = new RatingMethodologyDefinition(
        new RankingEligibilityPolicy(
            RankingEligibilityPolicy.InitialMethodologyVersion,
            provisionalMinUniqueContributors: 3,
            eligibleMinUniqueContributors: 10,
            establishedMinUniqueContributors: 30,
            strongEvidenceMinUniqueContributors: 100,
            minimumEligibleEntriesPerRanking: 3,
            minimumEligibleItemsForParkItemComponent: 5,
            minimumEligibleItemsPerCategory: 2,
            minimumEligibleCategories: 2,
            scoreTieEpsilon: 0.0001m),
        new DateOnly(2026, 8, 31),
        null,
        RatingMinimum: 0.5m,
        RatingMaximum: 5m,
        RatingStep: 0.5m,
        BayesianPriorMean: 3.5d,
        BayesianPriorWeight: 10,
        ParkDirectScoreWeight: 0.7d,
        ParkItemsScoreWeight: 0.3d,
        BalancesItemCategoriesEqually: true,
        RankingConvention: "competition");

    private static readonly IReadOnlyCollection<RatingMethodologyDefinition> Definitions =
        Array.AsReadOnly(new[] { Initial });

    public static RatingMethodologyDefinition Current => Initial;

    public static IReadOnlyCollection<RatingMethodologyDefinition> All => Definitions;

    public static bool TryResolve(
        RatingMethodologyVersion version,
        [NotNullWhen(true)] out RatingMethodologyDefinition? definition)
    {
        definition = Definitions.FirstOrDefault(candidate => candidate.Version == version);
        return definition is not null;
    }
}

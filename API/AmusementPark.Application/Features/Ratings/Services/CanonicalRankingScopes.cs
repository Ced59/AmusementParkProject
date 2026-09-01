using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

/// <summary>
/// Catalogue versionné des seuls scopes autorisés à produire un snapshot durable.
/// </summary>
public static class CanonicalRankingScopes
{
    public const string Version = "ranking-scopes-2026-01";

    private const int SnapshotPageSize = 500;

    public static RankingScopeDefinition GlobalParks { get; } = CreateGlobalParks();

    public static IReadOnlyList<RankingScopeDefinition> PublicItemCategories { get; } =
        Array.AsReadOnly(new[]
        {
            CreatePublicItemCategory(ParkItemCategory.Attraction, "attraction"),
            CreatePublicItemCategory(ParkItemCategory.Restaurant, "restaurant"),
            CreatePublicItemCategory(ParkItemCategory.Hotel, "hotel"),
            CreatePublicItemCategory(ParkItemCategory.Animal, "animal"),
            CreatePublicItemCategory(ParkItemCategory.Show, "show"),
            CreatePublicItemCategory(ParkItemCategory.Shop, "shop"),
            CreatePublicItemCategory(ParkItemCategory.Service, "service"),
            CreatePublicItemCategory(ParkItemCategory.Transport, "transport"),
        });

    public static IReadOnlyList<RankingScopeDefinition> All { get; } = BuildAll();

    private static RankingScopeDefinition CreateGlobalParks()
    {
        return new RankingScopeDefinition(
            RankingScopeKey.Parse("parks:global"),
            RankingTargetFamily.Parks,
            RankingFilterDefinition.Global,
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            RankingEligibilityPolicy.Initial.MinimumEligibleEntriesPerRanking,
            SnapshotPageSize,
            RankingEligibilityPolicy.Initial.ScoreTieEpsilon,
            RankingPublicationMode.DurableSnapshot);
    }

    private static RankingScopeDefinition CreatePublicItemCategory(
        ParkItemCategory category,
        string categoryKey)
    {
        return new RankingScopeDefinition(
            RankingScopeKey.Parse($"park-items:category:{categoryKey}"),
            RankingTargetFamily.ParkItems,
            RankingFilterDefinition.ForParkItemCategory(category),
            isPublic: true,
            RankingEligibilityPolicy.InitialMethodologyVersion,
            RankingEligibilityPolicy.Initial.MinimumEligibleEntriesPerRanking,
            SnapshotPageSize,
            RankingEligibilityPolicy.Initial.ScoreTieEpsilon,
            RankingPublicationMode.DurableSnapshot);
    }

    private static IReadOnlyList<RankingScopeDefinition> BuildAll()
    {
        RankingScopeDefinition[] definitions = new RankingScopeDefinition[PublicItemCategories.Count + 1];
        definitions[0] = GlobalParks;
        for (int index = 0; index < PublicItemCategories.Count; index++)
        {
            definitions[index + 1] = PublicItemCategories[index];
        }

        return Array.AsReadOnly(definitions);
    }
}

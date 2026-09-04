using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Filtre métier fermé d'un scope canonique. Il ne contient jamais de filtre HTTP libre.
/// </summary>
public sealed class RankingFilterDefinition
{
    private RankingFilterDefinition(RankingScopeFilterKind kind, ParkItemCategory? parkItemCategory)
    {
        this.Kind = kind;
        this.ParkItemCategory = parkItemCategory;
    }

    public static RankingFilterDefinition Global { get; } =
        new RankingFilterDefinition(RankingScopeFilterKind.Global, null);

    public RankingScopeFilterKind Kind { get; }

    public ParkItemCategory? ParkItemCategory { get; }

    public static RankingFilterDefinition ForParkItemCategory(ParkItemCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new ArgumentOutOfRangeException(nameof(category));
        }

        return new RankingFilterDefinition(RankingScopeFilterKind.ParkItemCategory, category);
    }
}

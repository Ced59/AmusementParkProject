using System.Diagnostics.CodeAnalysis;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Catalogue métier des méthodologies publiées, de la plus récente à la plus ancienne.
/// </summary>
public static class RatingMethodologyCatalog
{
    private static readonly RatingMethodologyDefinition Initial = new RatingMethodologyDefinition(
        RankingEligibilityPolicy.Initial,
        new DateOnly(2026, 8, 31),
        null);

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

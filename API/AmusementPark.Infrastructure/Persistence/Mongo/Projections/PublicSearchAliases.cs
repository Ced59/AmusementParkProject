namespace AmusementPark.Infrastructure.Persistence.Mongo.Projections;

internal static class PublicSearchAliases
{
    internal static IReadOnlyList<string> StandaloneAttractions { get; } = new[]
    {
        "standalone attraction",
        "isolated attraction",
        "attraction isolee",
    };

    internal static bool MatchesStandaloneAttractionTerm(string searchTerm)
    {
        string normalizedTerm = searchTerm.Trim();
        if (normalizedTerm.Length == 0)
        {
            return false;
        }

        return StandaloneAttractions.Any(alias => alias.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase));
    }
}

namespace AmusementPark.Infrastructure.Persistence.Mongo.Projections;

internal static class PublicSearchAliases
{
    internal const string CanonicalStandaloneAttraction = "standalone attraction";

    private static readonly IReadOnlyList<string> IndexedStandaloneAttractions = new[]
    {
        CanonicalStandaloneAttraction,
        "isolated attraction",
        "attraction isolee",
    };

    internal static IReadOnlyList<string> StandaloneAttractions { get; } = new[]
    {
        CanonicalStandaloneAttraction,
        "standalone attractions",
        "standalone attractions only",
        "isolated attraction",
        "isolated attractions",
        "attraction isolee",
        "attractions isolees",
        "attraction isolée",
        "attractions isolées",
        "attractions isolées seules",
        "eigenständige attraktion",
        "eigenständige attraktionen",
        "nur eigenständige attraktionen",
        "losse attractie",
        "losse attracties",
        "alleen losse attracties",
        "attrazione isolata",
        "attrazioni isolate",
        "solo attrazioni isolate",
        "atracción aislada",
        "atracciones aisladas",
        "solo atracciones aisladas",
        "samodzielna atrakcja",
        "samodzielne atrakcje",
        "tylko samodzielne atrakcje",
        "atração isolada",
        "atrações isoladas",
        "só atrações isoladas",
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

    internal static string NormalizeStandaloneAttractionTermForProjection(string searchTerm)
    {
        string normalizedTerm = searchTerm.Trim();
        if (normalizedTerm.Length == 0
            || IndexedStandaloneAttractions.Any(alias => alias.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase)))
        {
            return normalizedTerm;
        }

        return StandaloneAttractions.Any(alias => alias.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase))
            ? CanonicalStandaloneAttraction
            : normalizedTerm;
    }
}

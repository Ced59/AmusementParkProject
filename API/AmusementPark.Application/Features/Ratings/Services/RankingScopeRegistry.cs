using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public sealed class RankingScopeRegistry : IRankingScopeRegistry
{
    private readonly IReadOnlyDictionary<RankingScopeKey, RankingScopeDefinition> definitionsByKey;

    public RankingScopeRegistry(
        string version,
        IEnumerable<RankingScopeDefinition> definitions)
    {
        string normalizedVersion = version?.Trim() ?? string.Empty;
        if (normalizedVersion.Length == 0)
        {
            throw new ArgumentException("A ranking scope registry version is required.", nameof(version));
        }

        ArgumentNullException.ThrowIfNull(definitions);
        Dictionary<RankingScopeKey, RankingScopeDefinition> definitionsByKey =
            new Dictionary<RankingScopeKey, RankingScopeDefinition>();
        foreach (RankingScopeDefinition definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);
            if (!definitionsByKey.TryAdd(definition.Key, definition))
            {
                throw new ArgumentException(
                    $"The ranking scope '{definition.Key}' is defined more than once.",
                    nameof(definitions));
            }
        }

        if (definitionsByKey.Count == 0)
        {
            throw new ArgumentException("At least one ranking scope definition is required.", nameof(definitions));
        }

        this.Version = normalizedVersion;
        this.definitionsByKey = definitionsByKey;
        this.Definitions = Array.AsReadOnly(definitionsByKey.Values
            .OrderBy(static definition => definition.Key.Value, StringComparer.Ordinal)
            .ToArray());
    }

    public string Version { get; }

    public IReadOnlyCollection<RankingScopeDefinition> Definitions { get; }

    public bool TryResolve(
        string? scopeKey,
        RatingMethodologyVersion methodologyVersion,
        out RankingScopeDefinition? definition)
    {
        definition = null;
        if (!RankingScopeKey.TryParse(scopeKey, out RankingScopeKey parsedKey))
        {
            return false;
        }

        if (!this.definitionsByKey.TryGetValue(parsedKey, out RankingScopeDefinition? candidate))
        {
            return false;
        }

        try
        {
            if (candidate.MethodologyVersion != methodologyVersion ||
                !string.Equals(
                    candidate.MethodologyVersion.Value,
                    methodologyVersion.Value,
                    StringComparison.Ordinal))
            {
                return false;
            }
        }
        catch (InvalidOperationException)
        {
            return false;
        }

        definition = candidate;
        return true;
    }
}

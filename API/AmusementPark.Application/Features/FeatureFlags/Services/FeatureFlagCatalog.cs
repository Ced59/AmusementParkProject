using AmusementPark.Application.Features.FeatureFlags.Models;
using AmusementPark.Application.Features.FeatureFlags.Ports;

namespace AmusementPark.Application.Features.FeatureFlags.Services;

public sealed class FeatureFlagCatalog : IFeatureFlagCatalog
{
    private readonly IReadOnlyDictionary<string, FeatureFlagDefinition> definitions;

    public FeatureFlagCatalog()
    {
        FeatureFlagDefinition[] values =
        {
            new FeatureFlagDefinition(
                FeatureFlagKeys.LivePublicExperience,
                "Controls the public live wait, history and forecast experience.",
                "product-live",
                new DateOnly(2026, 9, 29),
                new DateOnly(2027, 3, 31),
                FeatureFlagKind.OperationalKillSwitch,
                true,
                false,
                new[] { "*" },
                Array.Empty<string>(),
                Array.Empty<string>(),
                "Public live availability, error rate and forecast eligibility.",
                "Hide live data while keeping park and attraction pages available.",
                "Remove the flag after the live capability is permanently accepted.",
                true),
        };

        this.definitions = values.ToDictionary(
            static definition => definition.Key,
            StringComparer.Ordinal);
        ValidateDependencies(this.definitions);
    }

    public IReadOnlyCollection<FeatureFlagDefinition> GetAll()
    {
        return this.definitions.Values
            .OrderBy(static definition => definition.Key, StringComparer.Ordinal)
            .ToArray();
    }

    public FeatureFlagDefinition? Find(string key)
    {
        return this.definitions.TryGetValue(key, out FeatureFlagDefinition? definition)
            ? definition
            : null;
    }

    private static void ValidateDependencies(
        IReadOnlyDictionary<string, FeatureFlagDefinition> definitions)
    {
        foreach (FeatureFlagDefinition definition in definitions.Values)
        {
            HashSet<string> path = new HashSet<string>(StringComparer.Ordinal);
            ValidateDependencyPath(definition.Key, definitions, path);
        }
    }

    private static void ValidateDependencyPath(
        string key,
        IReadOnlyDictionary<string, FeatureFlagDefinition> definitions,
        HashSet<string> path)
    {
        if (!path.Add(key))
        {
            throw new InvalidOperationException($"Feature flag dependency cycle detected at '{key}'.");
        }

        FeatureFlagDefinition definition = definitions[key];
        foreach (string dependency in definition.Dependencies)
        {
            if (!definitions.ContainsKey(dependency))
            {
                throw new InvalidOperationException(
                    $"Feature flag '{key}' depends on unknown flag '{dependency}'.");
            }

            ValidateDependencyPath(dependency, definitions, path);
        }

        path.Remove(key);
    }
}

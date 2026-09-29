namespace AmusementPark.Application.Features.FeatureFlags.Models;

public sealed record FeatureFlagDefinition(
    string Key,
    string Description,
    string Owner,
    DateOnly CreatedOn,
    DateOnly TargetRemovalOn,
    FeatureFlagKind Kind,
    bool DefaultEnabled,
    bool SafeFallbackEnabled,
    IReadOnlyCollection<string> Environments,
    IReadOnlyCollection<string> Cohorts,
    IReadOnlyCollection<string> Dependencies,
    string Metrics,
    string Fallback,
    string Cleanup,
    bool ExposeToClient);

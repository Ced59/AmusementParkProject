namespace AmusementPark.Application.Features.FeatureFlags.Models;

public sealed record FeatureFlagEvaluation(
    string Key,
    bool IsEnabled,
    FeatureFlagEvaluationSource Source,
    int Revision);

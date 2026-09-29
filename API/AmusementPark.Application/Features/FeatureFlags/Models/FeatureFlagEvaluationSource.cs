namespace AmusementPark.Application.Features.FeatureFlags.Models;

public enum FeatureFlagEvaluationSource
{
    Default = 1,
    Override = 2,
    Dependency = 3,
    SafeFallback = 4,
}

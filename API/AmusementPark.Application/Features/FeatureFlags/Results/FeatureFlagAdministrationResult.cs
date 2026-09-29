using AmusementPark.Application.Features.FeatureFlags.Models;

namespace AmusementPark.Application.Features.FeatureFlags.Results;

public sealed record FeatureFlagAdministrationResult(
    FeatureFlagDefinition Definition,
    string Environment,
    bool IsEnabled,
    FeatureFlagEvaluationSource EvaluationSource,
    bool? EnabledOverride,
    int Revision,
    string? Reason,
    string? ChangedByUserId,
    DateTime? RecordedAtUtc);

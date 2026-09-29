using AmusementPark.Application.Features.FeatureFlags.Results;
using AmusementPark.WebAPI.Contracts.FeatureFlags;

namespace AmusementPark.WebAPI.Mappers;

public static class FeatureFlagHttpMapper
{
    public static FeatureFlagAdministrationDto ToHttp(
        this FeatureFlagAdministrationResult result)
    {
        return new FeatureFlagAdministrationDto(
            result.Definition.Key,
            result.Definition.Description,
            result.Definition.Owner,
            result.Definition.CreatedOn,
            result.Definition.TargetRemovalOn,
            result.Definition.Kind.ToString(),
            result.Definition.DefaultEnabled,
            result.Definition.SafeFallbackEnabled,
            result.Definition.Environments,
            result.Definition.Cohorts,
            result.Definition.Dependencies,
            result.Definition.Metrics,
            result.Definition.Fallback,
            result.Definition.Cleanup,
            result.Definition.ExposeToClient,
            result.Environment,
            result.IsEnabled,
            result.EvaluationSource.ToString(),
            result.EnabledOverride,
            result.Revision,
            result.Reason,
            result.ChangedByUserId,
            result.RecordedAtUtc);
    }

    public static PublicFeatureCapabilityDto ToHttp(
        this PublicFeatureCapabilityResult result)
    {
        return new PublicFeatureCapabilityDto(result.Key, result.IsEnabled);
    }
}

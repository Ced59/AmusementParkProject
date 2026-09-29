namespace AmusementPark.WebAPI.Contracts.FeatureFlags;

public sealed record UpdateFeatureFlagRequestDto(
    bool? EnabledOverride,
    int ExpectedRevision,
    string Reason);

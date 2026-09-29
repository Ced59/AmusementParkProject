using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.FeatureFlags.Services;

public static class FeatureFlagApplicationErrors
{
    public static ApplicationError UnknownKey()
    {
        return ApplicationError.Validation(
            "feature-flag.unknown-key",
            "The feature flag key is unknown.");
    }

    public static ApplicationError InvalidReason()
    {
        return ApplicationError.Validation(
            "feature-flag.invalid-reason",
            "A reason between 10 and 500 characters is required.");
    }

    public static ApplicationError Conflict(int currentRevision)
    {
        return ApplicationError.Conflict(
            "feature-flag.conflict",
            "The feature flag changed in the meantime.",
            currentRevision);
    }

    public static ApplicationError StorageUnavailable()
    {
        return ApplicationError.Technical(
            "feature-flag.storage-unavailable",
            "Feature flag administration is temporarily unavailable.");
    }
}

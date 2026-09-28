namespace AmusementPark.Core.Domain.LiveData;

public static class LiveDataErrorCodes
{
    public const string InvalidEnum = "live-data.invalid-enum";

    public const string InvalidText = "live-data.invalid-text";

    public const string InvalidUri = "live-data.invalid-uri";

    public const string InvalidDuration = "live-data.invalid-duration";

    public const string InvalidTimestamp = "live-data.invalid-timestamp";

    public const string InconsistentTimeline = "live-data.inconsistent-timeline";

    public const string MissingAttribution = "live-data.missing-attribution";

    public const string InvalidFreshnessThresholds = "live-data.invalid-freshness-thresholds";

    public const string InvalidMapping = "live-data.invalid-mapping";

    public const string InvalidMappingTransition = "live-data.invalid-mapping-transition";

    public const string InvalidRevision = "live-data.invalid-revision";

    public const string MappingCountryMismatch = "live-data.mapping-country-mismatch";

    public const string MappingTargetTypeMismatch = "live-data.mapping-target-type-mismatch";

    public const string InvalidWaitTime = "live-data.invalid-wait-time";

    public const string InvalidQueue = "live-data.invalid-queue";

    public const string InvalidPollingPolicy = "live-data.invalid-polling-policy";
}

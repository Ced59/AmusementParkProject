namespace AmusementPark.WebAPI.Services;

public sealed class ParkDataEditorOperationCoordinationSnapshot
{
    public DateTime ServerTimeUtc { get; init; }

    public bool IsBusy { get; init; }

    public bool HasActiveExport { get; init; }

    public bool CanStartResourceIntensiveOperation { get; init; }

    public int ActiveRequestCount { get; init; }

    public int ActiveExportCount { get; init; }

    public int MaxConcurrentRequests { get; init; }

    public int MaxConcurrentResourceIntensiveOperations { get; init; }

    public int RecommendedPollIntervalSeconds { get; init; }

    public int RetryAfterSeconds { get; init; }

    public IReadOnlyCollection<ParkDataEditorActiveRequestSnapshot> ActiveRequests { get; init; } =
        Array.Empty<ParkDataEditorActiveRequestSnapshot>();
}

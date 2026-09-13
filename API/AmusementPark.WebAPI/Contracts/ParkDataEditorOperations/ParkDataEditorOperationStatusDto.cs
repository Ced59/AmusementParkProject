namespace AmusementPark.WebAPI.Contracts.ParkDataEditorOperations;

public sealed class ParkDataEditorOperationStatusDto
{
    public DateTime ServerTimeUtc { get; set; }

    public bool IsBusy { get; set; }

    public bool HasActiveExport { get; set; }

    public bool CanStartResourceIntensiveOperation { get; set; }

    public int ActiveRequestCount { get; set; }

    public int ActiveExportCount { get; set; }

    public int MaxConcurrentRequests { get; set; }

    public int MaxConcurrentResourceIntensiveOperations { get; set; }

    public int RecommendedPollIntervalSeconds { get; set; }

    public int RetryAfterSeconds { get; set; }

    public List<ParkDataEditorActiveRequestDto> ActiveRequests { get; set; } =
        new List<ParkDataEditorActiveRequestDto>();

    public List<ParkDataEditorActiveExportDto> ActiveExports { get; set; } =
        new List<ParkDataEditorActiveExportDto>();
}

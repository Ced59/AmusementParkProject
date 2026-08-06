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

public sealed class ParkDataEditorActiveRequestDto
{
    public string OperationId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public bool InitiatedByCurrentToken { get; set; }
}

public sealed class ParkDataEditorActiveExportDto
{
    public string JobId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public int ProgressPercentage { get; set; }

    public string? Message { get; set; }

    public int? ExportedParkCount { get; set; }

    public int? ProcessedParkCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public bool InitiatedByCurrentToken { get; set; }
}

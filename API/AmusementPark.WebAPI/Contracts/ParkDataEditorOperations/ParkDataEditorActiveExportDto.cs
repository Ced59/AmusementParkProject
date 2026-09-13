namespace AmusementPark.WebAPI.Contracts.ParkDataEditorOperations;

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

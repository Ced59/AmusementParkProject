using System.Text.Json;

namespace AmusementPark.WebAPI.Contracts.ParkGraphUpserts;

public sealed class ParkGraphBulkExportJobDto
{
    public string JobId { get; set; } = string.Empty;

    public string Status { get; set; } = "Queued";

    public int ProgressPercentage { get; set; }

    public string? Message { get; set; }

    public int? ExportedParkCount { get; set; }

    public int? ProcessedParkCount { get; set; }

    public string? FileName { get; set; }

    public long? ContentLength { get; set; }

    public string? DownloadUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public string? Error { get; set; }
}

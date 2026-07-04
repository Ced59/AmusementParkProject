namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class ParkGraphJsonExportProgress
{
    public string Step { get; init; } = "running";

    public int ProgressPercentage { get; init; }

    public int? ExportedParkCount { get; init; }

    public int? ProcessedParkCount { get; init; }

    public string? Message { get; init; }
}

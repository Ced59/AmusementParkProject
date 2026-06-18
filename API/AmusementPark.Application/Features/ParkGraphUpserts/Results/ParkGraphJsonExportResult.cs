namespace AmusementPark.Application.Features.ParkGraphUpserts.Results;

public sealed class ParkGraphJsonExportResult
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/json";

    public string Json { get; init; } = string.Empty;
}

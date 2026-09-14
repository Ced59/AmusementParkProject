namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSourceReportSearchRequestDto
{
    public int Page { get; init; } = 1;

    public int Size { get; init; } = 20;

    public string? Status { get; init; }

    public string? Reason { get; init; }

    public string? ParkId { get; init; }
}

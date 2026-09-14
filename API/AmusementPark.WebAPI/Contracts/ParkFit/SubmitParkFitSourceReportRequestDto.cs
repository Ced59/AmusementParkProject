namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class SubmitParkFitSourceReportRequestDto
{
    public string ParkId { get; init; } = string.Empty;

    public string EvidenceKind { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? SourceReference { get; init; }

    public string Reason { get; init; } = string.Empty;

    public string? Details { get; init; }
}

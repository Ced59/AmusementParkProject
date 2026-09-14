namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class CaptureParkFitPilotObservationRequestDto
{
    public string EventKind { get; init; } = string.Empty;

    public string? ResultBand { get; init; }

    public string? UnknownLevel { get; init; }

    public string? DurationBand { get; init; }

    public string? FailureKind { get; init; }

    public string? ComparisonSize { get; init; }

    public string? MethodVersion { get; init; }

    public IReadOnlyCollection<string> QualityIssues { get; init; } = Array.Empty<string>();
}

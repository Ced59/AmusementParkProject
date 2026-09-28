namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalPeriodRequestDto
{
    public HistoricalDateRequestDto? Start { get; init; }

    public HistoricalDateRequestDto? End { get; init; }

    public string StartConfidence { get; init; } = string.Empty;

    public string EndConfidence { get; init; } = string.Empty;
}

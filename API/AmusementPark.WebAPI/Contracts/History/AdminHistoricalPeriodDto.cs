namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalPeriodDto
{
    public AdminHistoricalDateDto? Start { get; init; }

    public AdminHistoricalDateDto? End { get; init; }

    public string StartConfidence { get; init; } = string.Empty;

    public string EndConfidence { get; init; } = string.Empty;
}

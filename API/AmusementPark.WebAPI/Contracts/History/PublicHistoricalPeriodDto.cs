namespace AmusementPark.WebAPI.Contracts.History;

public sealed class PublicHistoricalPeriodDto
{
    public PublicHistoricalDateDto? Start { get; set; }

    public PublicHistoricalDateDto? End { get; set; }

    public string StartConfidence { get; set; } = string.Empty;

    public string EndConfidence { get; set; } = string.Empty;
}

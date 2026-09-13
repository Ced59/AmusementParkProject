namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsCount
{
    public string Key { get; set; } = string.Empty;

    public long Count { get; set; }

    public double Percent { get; set; }
}

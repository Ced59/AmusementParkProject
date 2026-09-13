namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsCountDto
{
    public string Key { get; set; } = string.Empty;

    public long Count { get; set; }

    public double Percent { get; set; }
}

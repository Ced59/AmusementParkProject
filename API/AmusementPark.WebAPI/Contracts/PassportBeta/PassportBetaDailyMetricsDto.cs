namespace AmusementPark.WebAPI.Contracts.PassportBeta;

public sealed class PassportBetaDailyMetricsDto
{
    public string Date { get; set; } = string.Empty;

    public long CompletedVisits { get; set; }

    public long FirstVisits { get; set; }

    public long SecondVisits { get; set; }
}

namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitPilotDailyMetricsDto
{
    public string Date { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, long> EventCounts { get; init; } =
        new Dictionary<string, long>();

    public long SourceReports { get; init; }

    public long OutdatedSourceReports { get; init; }
}

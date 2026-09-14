namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitPilotMetricsDto
{
    public DateTime GeneratedAtUtc { get; init; }

    public DateTime FromUtc { get; init; }

    public DateTime ToUtc { get; init; }

    public long SearchesStarted { get; init; }

    public long SearchesCompleted { get; init; }

    public long SearchesFailed { get; init; }

    public long SearchesAbandoned { get; init; }

    public long ExplanationsViewed { get; init; }

    public long ComparisonsOpened { get; init; }

    public long SourceReports { get; init; }

    public long OutdatedSourceReports { get; init; }

    public ParkFitPilotHealthDto Health { get; init; } = new();

    public IReadOnlyDictionary<string, long> ResultBandCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> UnknownLevelCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> DurationBandCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> FailureKindCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> ComparisonSizeCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> QualityIssueCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> ZeroResultQualityIssueCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyDictionary<string, long> MethodVersionCounts { get; init; } =
        new Dictionary<string, long>();

    public IReadOnlyCollection<ParkFitPilotDailyMetricsDto> Daily { get; init; } =
        Array.Empty<ParkFitPilotDailyMetricsDto>();
}

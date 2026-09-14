namespace AmusementPark.Application.Features.ParkFit.Models;

public sealed record ParkFitPilotDailyMetrics(
    string Date,
    IReadOnlyDictionary<string, long> EventCounts,
    IReadOnlyDictionary<string, long> ResultBandCounts,
    IReadOnlyDictionary<string, long> UnknownLevelCounts,
    IReadOnlyDictionary<string, long> DurationBandCounts,
    IReadOnlyDictionary<string, long> FailureKindCounts,
    IReadOnlyDictionary<string, long> ComparisonSizeCounts,
    IReadOnlyDictionary<string, long> QualityIssueCounts,
    IReadOnlyDictionary<string, long> ZeroResultQualityIssueCounts,
    IReadOnlyDictionary<string, long> MethodVersionCounts,
    long SourceReports,
    long OutdatedSourceReports);

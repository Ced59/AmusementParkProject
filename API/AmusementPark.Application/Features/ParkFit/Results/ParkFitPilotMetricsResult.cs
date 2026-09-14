using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed record ParkFitPilotMetricsResult(
    DateTime GeneratedAtUtc,
    DateTime FromUtc,
    DateTime ToUtc,
    long SearchesStarted,
    long SearchesCompleted,
    long SearchesFailed,
    long SearchesAbandoned,
    long ExplanationsViewed,
    long ComparisonsOpened,
    long SourceReports,
    long OutdatedSourceReports,
    ParkFitPilotHealth Health,
    IReadOnlyDictionary<string, long> ResultBandCounts,
    IReadOnlyDictionary<string, long> UnknownLevelCounts,
    IReadOnlyDictionary<string, long> DurationBandCounts,
    IReadOnlyDictionary<string, long> FailureKindCounts,
    IReadOnlyDictionary<string, long> ComparisonSizeCounts,
    IReadOnlyDictionary<string, long> QualityIssueCounts,
    IReadOnlyDictionary<string, long> ZeroResultQualityIssueCounts,
    IReadOnlyDictionary<string, long> MethodVersionCounts,
    IReadOnlyCollection<ParkFitPilotDailyMetrics> Daily);

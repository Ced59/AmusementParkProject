using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record YearRecapSourceData(
    IReadOnlyCollection<PassportVisitStatisticsObservation> Visits,
    IReadOnlyCollection<PassportRideStatisticsObservation> Rides,
    IReadOnlyDictionary<string, string?> HistoricalItemNames,
    string SourceFingerprint,
    bool IsStable);

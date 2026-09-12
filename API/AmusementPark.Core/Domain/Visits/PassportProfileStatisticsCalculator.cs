namespace AmusementPark.Core.Domain.Visits;

public static class PassportProfileStatisticsCalculator
{
    public static PassportProfileStatistics Calculate(
        IReadOnlyCollection<PassportVisitStatisticsObservation> visits,
        IReadOnlyCollection<PassportRideStatisticsObservation> rides)
    {
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(rides);
        PassportScopeStatisticsCalculator.EnsureRideVisitsExist(visits, rides);

        PassportRideStatisticsObservation[] completedRides = rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed)
            .ToArray();
        IReadOnlyDictionary<int, long> completedRidesByYear = completedRides
            .GroupBy(static ride => ride.VisitDate.Year)
            .ToDictionary(static group => group.Key, static group => group.LongCount());
        IReadOnlyDictionary<string, long> completedRidesByPark = completedRides
            .GroupBy(static ride => ride.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.LongCount(),
                StringComparer.Ordinal);

        return new PassportProfileStatistics(
            visits.Select(static visit => visit.ParkId)
                .Distinct(StringComparer.Ordinal)
                .LongCount(),
            PassportScopeStatisticsCalculator.CalculateSummary(visits, rides),
            visits.GroupBy(static visit => visit.VisitDate.Year)
                .OrderByDescending(static group => group.Key)
                .Select(group => new PassportProfileYearStatistics(
                    group.Key,
                    group.LongCount(),
                    group.Select(static visit => visit.ParkId)
                        .Distinct(StringComparer.Ordinal)
                        .LongCount(),
                    completedRidesByYear.GetValueOrDefault(group.Key)))
                .ToArray(),
            visits.GroupBy(static visit => visit.ParkId, StringComparer.Ordinal)
                .Select(group =>
                {
                    PassportVisitStatisticsObservation[] parkVisits = group.ToArray();
                    double[] ratings = parkVisits
                        .Where(static visit => visit.ParkAssessment.HasValue)
                        .Select(static visit => visit.ParkAssessment!.Value.DoubleValue)
                        .ToArray();
                    return new PassportProfileParkStatistics(
                        group.Key,
                        parkVisits.LongLength,
                        parkVisits.Min(static visit => visit.VisitDate.Year),
                        parkVisits.Max(static visit => visit.VisitDate.Year),
                        completedRidesByPark.GetValueOrDefault(group.Key),
                        ratings.LongLength,
                        ratings.Length == 0 ? null : ratings.Average());
                })
                .OrderByDescending(static park => park.VisitCount)
                .ThenBy(static park => park.ParkId, StringComparer.Ordinal)
                .ToArray());
    }
}

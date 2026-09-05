namespace AmusementPark.Core.Domain.Visits;

public static class PassportGlobalStatisticsCalculator
{
    public const int RankingLimit = 10;

    public static PassportGlobalStatistics Calculate(
        IReadOnlyCollection<PassportVisitStatisticsObservation> visits,
        IReadOnlyCollection<PassportRideStatisticsObservation> rides)
    {
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(rides);
        PassportScopeStatisticsCalculator.EnsureRideVisitsExist(visits, rides);

        PassportRideStatisticsObservation[] completedRides = rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed)
            .ToArray();
        IReadOnlyDictionary<int, long> rideCountsByYear = rides
            .GroupBy(static ride => ride.VisitDate.Year)
            .ToDictionary(static group => group.Key, static group => group.LongCount());
        IReadOnlyDictionary<string, long> rideCountsByPark = rides
            .GroupBy(static ride => ride.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.LongCount(),
                StringComparer.Ordinal);

        return new PassportGlobalStatistics(
            visits.Select(static visit => visit.ParkId)
                .Distinct(StringComparer.Ordinal)
                .LongCount(),
            PassportScopeStatisticsCalculator.CalculateSummary(visits, rides),
            visits.GroupBy(static visit => visit.VisitDate.Year)
                .OrderBy(static group => group.Key)
                .Select(group => new PassportGlobalYearActivity(
                    group.Key,
                    group.LongCount(),
                    rideCountsByYear.GetValueOrDefault(group.Key)))
                .ToArray(),
            visits.GroupBy(static visit => visit.ParkId, StringComparer.Ordinal)
                .Select(group => new PassportGlobalParkActivity(
                    group.Key,
                    group.LongCount(),
                    rideCountsByPark.GetValueOrDefault(group.Key)))
                .OrderByDescending(static item => item.VisitCount)
                .ThenByDescending(static item => item.RecordedRideCount)
                .ThenBy(static item => item.ParkId, StringComparer.Ordinal)
                .Take(RankingLimit)
                .ToArray(),
            completedRides.GroupBy(static ride => ride.ParkItemId, StringComparer.Ordinal)
                .Select(group => new PassportGlobalItemActivity(
                    group.Key,
                    group.First().ParkId,
                    group.LongCount()))
                .OrderByDescending(static item => item.CompletedRideCount)
                .ThenBy(static item => item.ParkItemId, StringComparer.Ordinal)
                .Take(RankingLimit)
                .ToArray(),
            BuildRatingEvolution(visits, completedRides));
    }

    private static IReadOnlyCollection<PassportGlobalRatingEvolution> BuildRatingEvolution(
        IReadOnlyCollection<PassportVisitStatisticsObservation> visits,
        IReadOnlyCollection<PassportRideStatisticsObservation> completedRides)
    {
        IReadOnlyDictionary<int, double[]> parkRatingsByYear = visits
            .Where(static visit => visit.ParkAssessment.HasValue)
            .GroupBy(static visit => visit.VisitDate.Year)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(
                    static visit => visit.ParkAssessment!.Value.DoubleValue).ToArray());
        IReadOnlyDictionary<int, double[]> rideRatingsByYear = completedRides
            .Where(static ride => ride.Assessment.HasValue)
            .GroupBy(static ride => ride.VisitDate.Year)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(
                    static ride => ride.Assessment!.Value.DoubleValue).ToArray());

        return visits.Select(static visit => visit.VisitDate.Year)
            .Concat(completedRides.Select(static ride => ride.VisitDate.Year))
            .Distinct()
            .OrderBy(static year => year)
            .Select(year =>
            {
                double[] parkRatings = parkRatingsByYear.GetValueOrDefault(year) ?? Array.Empty<double>();
                double[] rideRatings = rideRatingsByYear.GetValueOrDefault(year) ?? Array.Empty<double>();
                return new PassportGlobalRatingEvolution(
                    year,
                    parkRatings.Length == 0 ? null : parkRatings.Average(),
                    parkRatings.LongLength,
                    rideRatings.Length == 0 ? null : rideRatings.Average(),
                    rideRatings.LongLength);
            })
            .ToArray();
    }
}

using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public static class PassportScopeStatisticsCalculator
{
    public const int TopItemLimit = 10;

    public static PassportParkStatistics CalculatePark(
        string parkId,
        IReadOnlyCollection<PassportVisitStatisticsObservation> visits,
        IReadOnlyCollection<PassportRideStatisticsObservation> rides,
        RatingValue? currentGlobalRating,
        IReadOnlyCollection<PassportCurrentItemRatingObservation> currentItemRatings)
    {
        string normalizedParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(rides);
        ArgumentNullException.ThrowIfNull(currentItemRatings);
        EnsureParkScope(normalizedParkId, visits, rides);
        EnsureRideVisitsExist(visits, rides);

        PassportStatisticsSummary summary = CalculateSummary(visits, rides);
        PassportCurrentItemRating[] currentTop = currentItemRatings
            .GroupBy(static rating => rating.ParkItemId, StringComparer.Ordinal)
            .Select(static group => new PassportCurrentItemRating(
                group.Key,
                group.OrderByDescending(static rating => rating.Rating.HalfSteps)
                    .First().Rating))
            .OrderByDescending(static rating => rating.Rating.HalfSteps)
            .ThenBy(static rating => rating.ParkItemId, StringComparer.Ordinal)
            .Take(TopItemLimit)
            .ToArray();

        return new PassportParkStatistics(
            normalizedParkId,
            summary,
            currentGlobalRating,
            currentGlobalRating.HasValue && summary.ParkRatings is not null
                ? currentGlobalRating.Value.DoubleValue - summary.ParkRatings.Average
                : null,
            BuildAssessmentTimeline(visits),
            visits.GroupBy(static visit => visit.VisitDate.Year)
                .OrderBy(static group => group.Key)
                .Select(group => new PassportYearBreakdown(
                    group.Key,
                    CalculateSummary(
                        group.ToArray(),
                        rides.Where(ride => ride.VisitDate.Year == group.Key).ToArray())))
                .ToArray(),
            currentTop,
            BuildHistoricalTop(rides));
    }

    public static PassportYearStatistics CalculateYear(
        int year,
        IReadOnlyCollection<PassportVisitStatisticsObservation> visits,
        IReadOnlyCollection<PassportRideStatisticsObservation> rides)
    {
        if (year < DateOnly.MinValue.Year || year > DateOnly.MaxValue.Year)
        {
            throw new ArgumentOutOfRangeException(nameof(year));
        }

        ArgumentNullException.ThrowIfNull(visits);
        ArgumentNullException.ThrowIfNull(rides);
        if (visits.Any(visit => visit.VisitDate.Year != year)
            || rides.Any(ride => ride.VisitDate.Year != year))
        {
            throw new ArgumentException("Every observation must belong to the requested year.");
        }

        EnsureRideVisitsExist(visits, rides);

        return new PassportYearStatistics(
            year,
            visits.Select(static visit => visit.ParkId)
                .Distinct(StringComparer.Ordinal)
                .LongCount(),
            CalculateSummary(visits, rides),
            visits.GroupBy(static visit => visit.ParkId, StringComparer.Ordinal)
                .OrderBy(static group => group.Key, StringComparer.Ordinal)
                .Select(group => new PassportParkBreakdown(
                    group.Key,
                    CalculateSummary(
                        group.ToArray(),
                        rides.Where(ride => string.Equals(
                            ride.ParkId,
                            group.Key,
                            StringComparison.Ordinal)).ToArray())))
                .ToArray());
    }

    internal static PassportStatisticsSummary CalculateSummary(
        IReadOnlyCollection<PassportVisitStatisticsObservation> visits,
        IReadOnlyCollection<PassportRideStatisticsObservation> rides)
    {
        PassportVisitStatisticsObservation[] orderedVisits = visits
            .OrderBy(static visit => visit.VisitDate.ChronologicalOrderValue)
            .ThenBy(static visit => visit.VisitId, StringComparer.Ordinal)
            .ToArray();
        RatingValue[] parkRatings = visits
            .Where(static visit => visit.ParkAssessment.HasValue)
            .Select(static visit => visit.ParkAssessment!.Value)
            .ToArray();
        PassportRideStatisticsObservation[] completedRides = rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed)
            .ToArray();
        RatingValue[] rideRatings = completedRides
            .Where(static ride => ride.Assessment.HasValue)
            .Select(static ride => ride.Assessment!.Value)
            .ToArray();
        long visitCount = visits.Count;
        long completedRideCount = completedRides.LongLength;

        return new PassportStatisticsSummary(
            visitCount,
            visits.LongCount(static visit => visit.VisitDate.IsApproximate),
            parkRatings.LongLength,
            Divide(parkRatings.LongLength, visitCount),
            PassportRatingStatisticsCalculator.Calculate(parkRatings),
            orderedVisits.Length == 0 ? null : ToExperience(orderedVisits[0]),
            orderedVisits.Length == 0 ? null : ToExperience(orderedVisits[^1]),
            new PassportRideOutcomeStatistics(
                rides.Count,
                completedRideCount,
                rides.LongCount(static ride => ride.Status == RideOccurrenceStatus.Attempted),
                rides.LongCount(static ride => ride.Status == RideOccurrenceStatus.MissedClosed),
                rides.LongCount(static ride => ride.Status == RideOccurrenceStatus.MissedUnavailable),
                rides.LongCount(static ride => ride.Status == RideOccurrenceStatus.SkippedByChoice)),
            rideRatings.LongLength,
            Divide(rideRatings.LongLength, completedRideCount),
            PassportRatingStatisticsCalculator.Calculate(rideRatings),
            completedRides.Select(static ride => ride.ParkItemId)
                .Distinct(StringComparer.Ordinal)
                .LongCount(),
            completedRides.GroupBy(static ride => ride.ParkItemId, StringComparer.Ordinal)
                .LongCount(static group => group.LongCount() > 1),
            BuildCategoryCoverage(completedRides));
    }

    private static IReadOnlyCollection<PassportCategoryCoverage> BuildCategoryCoverage(
        IReadOnlyCollection<PassportRideStatisticsObservation> completedRides)
    {
        long denominator = completedRides.Count;
        return completedRides
            .Select(static ride => new CategorizedRide(
                ride,
                ride.HistoricalCategory ?? ride.CurrentCategory,
                ride.HistoricalCategory is not null,
                ride.HistoricalCategory is null && ride.CurrentCategory is not null))
            .GroupBy(static ride => ride.Category, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => group.Key is null)
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PassportCategoryCoverage(
                group.Key,
                group.LongCount(),
                group.Select(static value => value.Ride.ParkItemId)
                    .Distinct(StringComparer.Ordinal)
                    .LongCount(),
                group.LongCount(static value => value.UsesHistoricalCategory),
                group.LongCount(static value => value.UsesCurrentCategory),
                group.LongCount(static value => value.Category is null),
                Divide(group.LongCount(), denominator)))
            .ToArray();
    }

    private static IReadOnlyCollection<PassportParkAssessmentPoint> BuildAssessmentTimeline(
        IEnumerable<PassportVisitStatisticsObservation> visits)
    {
        return visits
            .Where(static visit => visit.ParkAssessment.HasValue)
            .OrderBy(static visit => visit.VisitDate.ChronologicalOrderValue)
            .ThenBy(static visit => visit.VisitId, StringComparer.Ordinal)
            .Select(static visit => new PassportParkAssessmentPoint(
                visit.VisitId,
                visit.VisitDate,
                visit.ParkAssessment!.Value))
            .ToArray();
    }

    private static IReadOnlyCollection<PassportHistoricalItemRating> BuildHistoricalTop(
        IEnumerable<PassportRideStatisticsObservation> rides)
    {
        return rides
            .Where(static ride => ride.Status == RideOccurrenceStatus.Completed
                && ride.Assessment.HasValue)
            .GroupBy(static ride => ride.ParkItemId, StringComparer.Ordinal)
            .Select(static group => new PassportHistoricalItemRating(
                group.Key,
                group.LongCount(),
                group.Average(static ride => ride.Assessment!.Value.DoubleValue)))
            .OrderByDescending(static rating => rating.Average)
            .ThenByDescending(static rating => rating.RatingCount)
            .ThenBy(static rating => rating.ParkItemId, StringComparer.Ordinal)
            .Take(TopItemLimit)
            .ToArray();
    }

    private static PassportVisitExperience ToExperience(
        PassportVisitStatisticsObservation visit)
    {
        return new PassportVisitExperience(visit.VisitId, visit.ParkId, visit.VisitDate);
    }

    private static double Divide(long numerator, long denominator)
    {
        return denominator == 0 ? 0d : numerator / (double)denominator;
    }

    private static void EnsureParkScope(
        string parkId,
        IEnumerable<PassportVisitStatisticsObservation> visits,
        IEnumerable<PassportRideStatisticsObservation> rides)
    {
        if (visits.Any(visit => !string.Equals(
                visit.ParkId,
                parkId,
                StringComparison.Ordinal))
            || rides.Any(ride => !string.Equals(
                ride.ParkId,
                parkId,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException("Every observation must belong to the requested park.");
        }
    }

    internal static void EnsureRideVisitsExist(
        IEnumerable<PassportVisitStatisticsObservation> visits,
        IEnumerable<PassportRideStatisticsObservation> rides)
    {
        HashSet<string> visitIds = visits
            .Select(static visit => visit.VisitId)
            .ToHashSet(StringComparer.Ordinal);
        if (rides.Any(ride => !visitIds.Contains(ride.VisitId)))
        {
            throw new ArgumentException("Every ride observation must reference a visit in scope.");
        }
    }
}

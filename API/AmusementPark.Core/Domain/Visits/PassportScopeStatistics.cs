using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportVisitStatisticsObservation
{
    public PassportVisitStatisticsObservation(
        string visitId,
        string parkId,
        VisitDate visitDate,
        RatingValue? parkAssessment)
    {
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        this.ParkAssessment = parkAssessment;
    }

    public string VisitId { get; }

    public string ParkId { get; }

    public VisitDate VisitDate { get; }

    public RatingValue? ParkAssessment { get; }
}

public sealed record PassportRideStatisticsObservation
{
    public PassportRideStatisticsObservation(
        string rideOccurrenceId,
        string visitId,
        string parkId,
        string parkItemId,
        VisitDate visitDate,
        RideOccurrenceStatus status,
        RatingValue? assessment,
        string? historicalCategory,
        string? currentCategory)
    {
        this.RideOccurrenceId = IdentifierRules.NormalizeRequired(
            rideOccurrenceId,
            nameof(rideOccurrenceId));
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        this.Status = status;
        this.Assessment = assessment;
        this.HistoricalCategory = NormalizeOptional(historicalCategory);
        this.CurrentCategory = NormalizeOptional(currentCategory);
    }

    public string RideOccurrenceId { get; }

    public string VisitId { get; }

    public string ParkId { get; }

    public string ParkItemId { get; }

    public VisitDate VisitDate { get; }

    public RideOccurrenceStatus Status { get; }

    public RatingValue? Assessment { get; }

    public string? HistoricalCategory { get; }

    public string? CurrentCategory { get; }

    private static string? NormalizeOptional(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        return normalized.Length == 0 ? null : normalized;
    }
}

public sealed record PassportCurrentItemRatingObservation
{
    public PassportCurrentItemRatingObservation(string parkItemId, RatingValue rating)
    {
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.Rating = rating;
    }

    public string ParkItemId { get; }

    public RatingValue Rating { get; }
}

public sealed record PassportVisitExperience(
    string VisitId,
    string ParkId,
    VisitDate VisitDate);

public sealed record PassportRideOutcomeStatistics(
    long RecordedOutcomeCount,
    long CompletedRideCount,
    long AttemptedCount,
    long MissedClosedCount,
    long MissedUnavailableCount,
    long SkippedByChoiceCount);

public sealed record PassportCategoryCoverage(
    string? Category,
    long CompletedRideCount,
    long DistinctItemCount,
    long HistoricalReferenceRideCount,
    long CurrentReferenceRideCount,
    long UnknownReferenceRideCount,
    double CompletedRideRate);

public sealed record PassportStatisticsSummary(
    long VisitCount,
    long ApproximateVisitCount,
    long RatedVisitCount,
    double ParkRatingCoverageRate,
    PassportRatingStatistics? ParkRatings,
    PassportVisitExperience? FirstVisit,
    PassportVisitExperience? LastVisit,
    PassportRideOutcomeStatistics RideOutcomes,
    long RatedRideCount,
    double RideRatingCoverageRate,
    PassportRatingStatistics? RideRatings,
    long DistinctCompletedItemCount,
    long RepeatedCompletedItemCount,
    IReadOnlyCollection<PassportCategoryCoverage> CategoryCoverage);

public sealed record PassportParkAssessmentPoint(
    string VisitId,
    VisitDate VisitDate,
    RatingValue Rating);

public sealed record PassportCurrentItemRating(
    string ParkItemId,
    RatingValue Rating);

public sealed record PassportHistoricalItemRating(
    string ParkItemId,
    long RatingCount,
    double Average);

public sealed record PassportYearBreakdown(
    int Year,
    PassportStatisticsSummary Summary);

public sealed record PassportParkBreakdown(
    string ParkId,
    PassportStatisticsSummary Summary);

public sealed record PassportParkStatistics(
    string ParkId,
    PassportStatisticsSummary Summary,
    RatingValue? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportParkAssessmentPoint> AssessmentTimeline,
    IReadOnlyCollection<PassportYearBreakdown> ByYear,
    IReadOnlyCollection<PassportCurrentItemRating> CurrentTopItems,
    IReadOnlyCollection<PassportHistoricalItemRating> HistoricalTopItems);

public sealed record PassportYearStatistics(
    int Year,
    long ParkCount,
    PassportStatisticsSummary Summary,
    IReadOnlyCollection<PassportParkBreakdown> ByPark);

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

    private sealed record CategorizedRide(
        PassportRideStatisticsObservation Ride,
        string? Category,
        bool UsesHistoricalCategory,
        bool UsesCurrentCategory);
}

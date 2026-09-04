using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Observation active d'un tour terminé, suffisante pour les statistiques privées d'un élément.
/// </summary>
public sealed record PassportItemRideObservation
{
    public PassportItemRideObservation(
        string rideOccurrenceId,
        string visitId,
        VisitDate visitDate,
        long sortPosition,
        RatingValue? assessment)
    {
        this.RideOccurrenceId = IdentifierRules.NormalizeRequired(
            rideOccurrenceId,
            nameof(rideOccurrenceId));
        this.VisitId = IdentifierRules.NormalizeRequired(visitId, nameof(visitId));
        this.VisitDate = visitDate ?? throw new ArgumentNullException(nameof(visitDate));
        this.SortPosition = sortPosition;
        this.Assessment = assessment;
    }

    public string RideOccurrenceId { get; }

    public string VisitId { get; }

    public VisitDate VisitDate { get; }

    public long SortPosition { get; }

    public RatingValue? Assessment { get; }
}

public sealed record PassportItemExperience(
    string VisitId,
    VisitDate VisitDate);

public sealed record PassportItemRatingPoint(
    string RideOccurrenceId,
    string VisitId,
    VisitDate VisitDate,
    long SortPosition,
    RatingValue Rating);

public sealed record PassportItemVisitStatistics(
    string VisitId,
    VisitDate VisitDate,
    long RideCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportRatingStatistics? Ratings);

public sealed record PassportItemYearStatistics(
    int Year,
    long RideCount,
    long VisitCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportRatingStatistics? Ratings);

public enum PassportRatingTrendKind
{
    Stable = 0,
    Rising = 1,
    Falling = 2,
}

public sealed record PassportRatingTrend(
    PassportRatingTrendKind Kind,
    long FirstWindowRatingCount,
    long LastWindowRatingCount,
    double FirstWindowAverage,
    double LastWindowAverage,
    double Delta);

public sealed record PassportItemStatistics(
    long RideCount,
    long VisitCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportItemExperience? FirstExperience,
    PassportItemExperience? LastExperience,
    PassportRatingStatistics? Ratings,
    RatingValue? CurrentGlobalRating,
    double? CurrentGlobalMinusHistoricalAverage,
    IReadOnlyCollection<PassportItemVisitStatistics> ByVisit,
    IReadOnlyCollection<PassportItemYearStatistics> ByYear,
    IReadOnlyCollection<PassportItemRatingPoint> RatingTimeline,
    PassportRatingTrend? Trend);

/// <summary>
/// Calcule les statistiques d'un élément à partir des observations actives.
/// Les observations représentent toute la population privée connue : la dispersion est donc
/// l'écart-type de population. Les dates partielles restent des <see cref="VisitDate"/>.
/// </summary>
public static class PassportItemStatisticsCalculator
{
    public static PassportItemStatistics Calculate(
        IReadOnlyCollection<PassportItemRideObservation> observations,
        RatingValue? currentGlobalRating)
    {
        ArgumentNullException.ThrowIfNull(observations);
        if (observations.Count == 0)
        {
            return new PassportItemStatistics(
                0,
                0,
                0,
                0d,
                null,
                null,
                null,
                currentGlobalRating,
                null,
                Array.Empty<PassportItemVisitStatistics>(),
                Array.Empty<PassportItemYearStatistics>(),
                Array.Empty<PassportItemRatingPoint>(),
                null);
        }

        PassportItemRideObservation[] orderedObservations = observations
            .OrderBy(static observation => observation.VisitDate.ChronologicalOrderValue)
            .ThenBy(static observation => observation.VisitId, StringComparer.Ordinal)
            .ThenBy(static observation => observation.SortPosition)
            .ThenBy(static observation => observation.RideOccurrenceId, StringComparer.Ordinal)
            .ToArray();
        RatingValue[] ratingsSource = observations
            .Where(static observation => observation.Assessment.HasValue)
            .Select(static observation => observation.Assessment!.Value)
            .ToArray();
        long rideCount = observations.Count;
        long ratedRideCount = ratingsSource.LongLength;
        PassportRatingStatistics? ratings =
            PassportRatingStatisticsCalculator.Calculate(ratingsSource);

        IReadOnlyCollection<PassportItemRatingPoint> timeline =
            BuildTimeline(orderedObservations);
        return new PassportItemStatistics(
            rideCount,
            observations.Select(static observation => observation.VisitId)
                .Distinct(StringComparer.Ordinal)
                .LongCount(),
            ratedRideCount,
            ratedRideCount / (double)rideCount,
            ToExperience(orderedObservations[0]),
            ToExperience(orderedObservations[^1]),
            ratings,
            currentGlobalRating,
            currentGlobalRating.HasValue && ratings is not null
                ? currentGlobalRating.Value.DoubleValue - ratings.Average
                : null,
            BuildVisitStatistics(orderedObservations),
            BuildYearStatistics(observations),
            timeline,
            BuildTrend(timeline));
    }

    private static PassportItemExperience ToExperience(
        PassportItemRideObservation observation)
    {
        return new PassportItemExperience(observation.VisitId, observation.VisitDate);
    }

    private static IReadOnlyCollection<PassportItemVisitStatistics> BuildVisitStatistics(
        IReadOnlyCollection<PassportItemRideObservation> orderedObservations)
    {
        return orderedObservations
            .GroupBy(static observation => observation.VisitId, StringComparer.Ordinal)
            .Select(static group =>
            {
                PassportItemRideObservation[] rides = group.ToArray();
                RatingValue[] ratings = rides
                    .Where(static ride => ride.Assessment.HasValue)
                    .Select(static ride => ride.Assessment!.Value)
                    .ToArray();
                return new PassportItemVisitStatistics(
                    group.Key,
                    rides[0].VisitDate,
                    rides.LongLength,
                    ratings.LongLength,
                    ratings.LongLength / (double)rides.LongLength,
                    PassportRatingStatisticsCalculator.Calculate(ratings));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<PassportItemYearStatistics> BuildYearStatistics(
        IReadOnlyCollection<PassportItemRideObservation> observations)
    {
        return observations
            .GroupBy(static observation => observation.VisitDate.Year)
            .OrderBy(static group => group.Key)
            .Select(static group =>
            {
                PassportItemRideObservation[] rides = group.ToArray();
                RatingValue[] ratings = rides
                    .Where(static ride => ride.Assessment.HasValue)
                    .Select(static ride => ride.Assessment!.Value)
                    .ToArray();
                return new PassportItemYearStatistics(
                    group.Key,
                    rides.LongLength,
                    rides.Select(static ride => ride.VisitId)
                        .Distinct(StringComparer.Ordinal)
                        .LongCount(),
                    ratings.LongLength,
                    ratings.LongLength / (double)rides.LongLength,
                    PassportRatingStatisticsCalculator.Calculate(ratings));
            })
            .ToArray();
    }

    private static IReadOnlyCollection<PassportItemRatingPoint> BuildTimeline(
        IEnumerable<PassportItemRideObservation> orderedObservations)
    {
        List<PassportItemRatingPoint> points = new List<PassportItemRatingPoint>();
        foreach (PassportItemRideObservation observation in orderedObservations)
        {
            if (!observation.Assessment.HasValue)
            {
                continue;
            }

            points.Add(new PassportItemRatingPoint(
                observation.RideOccurrenceId,
                observation.VisitId,
                observation.VisitDate,
                observation.SortPosition,
                observation.Assessment.Value));
        }

        return points;
    }

    private static PassportRatingTrend? BuildTrend(
        IReadOnlyCollection<PassportItemRatingPoint> timeline)
    {
        if (timeline.Count < 3
            || timeline.Select(static point => point.VisitId)
                .Distinct(StringComparer.Ordinal)
                .Count() < 2)
        {
            return null;
        }

        PassportItemRatingPoint[] points = timeline.ToArray();
        int windowSize = points.Length / 2;
        RatingValue[] firstWindow = points.Take(windowSize)
            .Select(static point => point.Rating)
            .ToArray();
        RatingValue[] lastWindow = points.TakeLast(windowSize)
            .Select(static point => point.Rating)
            .ToArray();
        PassportRatingStatistics firstStatistics =
            PassportRatingStatisticsCalculator.Calculate(firstWindow)!;
        PassportRatingStatistics lastStatistics =
            PassportRatingStatisticsCalculator.Calculate(lastWindow)!;
        double delta = lastStatistics.Average - firstStatistics.Average;
        PassportRatingTrendKind kind = delta > 0.5d
            ? PassportRatingTrendKind.Rising
            : delta < -0.5d
                ? PassportRatingTrendKind.Falling
                : PassportRatingTrendKind.Stable;
        return new PassportRatingTrend(
            kind,
            firstStatistics.RatingCount,
            lastStatistics.RatingCount,
            firstStatistics.Average,
            lastStatistics.Average,
            delta);
    }
}

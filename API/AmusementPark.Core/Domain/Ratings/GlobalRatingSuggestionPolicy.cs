namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Explique pourquoi des observations privées invitent à revoir une note globale.
/// </summary>
public enum GlobalRatingSuggestionReason
{
    RecentExperiencesLower = 1,
    RecentExperiencesHigher = 2,
}

/// <summary>
/// Observation privée datée utilisée par la politique de suggestion.
/// </summary>
public sealed record GlobalRatingSuggestionObservation(
    RatingValue Value,
    DateTime RecordedAtUtc);

/// <summary>
/// État de cadence propre à une cible. Il ne contient aucune valeur de note.
/// </summary>
public sealed record GlobalRatingSuggestionCadence(
    bool IsEnabled,
    DateTime? LastPresentedAtUtc);

public sealed record GlobalRatingSuggestionEvaluation(
    GlobalRatingSuggestionReason Reason,
    int NewObservationCount,
    int RecentObservationCount,
    RatingValue LatestObservation,
    double RecentAverage,
    double HistoricalMedian,
    DateTime LatestObservationAtUtc);

/// <summary>
/// Politique pure et déterministe. Elle ne modifie jamais la note globale.
/// </summary>
public sealed class GlobalRatingSuggestionPolicy
{
    public const int MinimumNewObservationCount = 2;

    public const int RecentObservationLimit = 3;

    public const int MinimumDifferenceHalfSteps = 2;

    public static readonly TimeSpan PresentationCooldown = TimeSpan.FromDays(30);

    public GlobalRatingSuggestionEvaluation? Evaluate(
        RatingValue currentGlobalRating,
        DateTime currentGlobalRatingUpdatedAtUtc,
        IReadOnlyCollection<GlobalRatingSuggestionObservation> observations,
        GlobalRatingSuggestionCadence cadence,
        DateTime nowUtc)
    {
        ValidateUtc(currentGlobalRatingUpdatedAtUtc, nameof(currentGlobalRatingUpdatedAtUtc));
        ValidateUtc(nowUtc, nameof(nowUtc));
        ArgumentNullException.ThrowIfNull(observations);
        _ = currentGlobalRating.HalfSteps;
        if (cadence.LastPresentedAtUtc.HasValue)
        {
            ValidateUtc(cadence.LastPresentedAtUtc.Value, nameof(cadence.LastPresentedAtUtc));
        }

        if (!cadence.IsEnabled
            || cadence.LastPresentedAtUtc.HasValue
            && cadence.LastPresentedAtUtc.Value.Add(PresentationCooldown) > nowUtc)
        {
            return null;
        }

        GlobalRatingSuggestionObservation[] ordered = observations
            .Select(ValidateObservation)
            .OrderByDescending(static observation => observation.RecordedAtUtc)
            .ToArray();
        GlobalRatingSuggestionObservation[] newObservations = ordered
            .Where(observation => observation.RecordedAtUtc > currentGlobalRatingUpdatedAtUtc)
            .ToArray();
        if (newObservations.Length < MinimumNewObservationCount)
        {
            return null;
        }

        GlobalRatingSuggestionObservation[] recent = newObservations
            .Take(RecentObservationLimit)
            .ToArray();
        double recentAverage = recent.Average(static observation => observation.Value.DoubleValue);
        double minimumDifference = MinimumDifferenceHalfSteps / 2d;
        double difference = recentAverage - currentGlobalRating.DoubleValue;
        if (Math.Abs(difference) < minimumDifference)
        {
            return null;
        }

        return new GlobalRatingSuggestionEvaluation(
            difference < 0d
                ? GlobalRatingSuggestionReason.RecentExperiencesLower
                : GlobalRatingSuggestionReason.RecentExperiencesHigher,
            newObservations.Length,
            recent.Length,
            recent[0].Value,
            recentAverage,
            CalculateMedian(ordered),
            recent[0].RecordedAtUtc);
    }

    private static GlobalRatingSuggestionObservation ValidateObservation(
        GlobalRatingSuggestionObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        _ = observation.Value.HalfSteps;
        ValidateUtc(observation.RecordedAtUtc, nameof(observation.RecordedAtUtc));
        return observation;
    }

    private static double CalculateMedian(
        IReadOnlyCollection<GlobalRatingSuggestionObservation> observations)
    {
        double[] orderedValues = observations
            .Select(static observation => observation.Value.DoubleValue)
            .OrderBy(static value => value)
            .ToArray();
        int middle = orderedValues.Length / 2;
        return orderedValues.Length % 2 == 0
            ? (orderedValues[middle - 1] + orderedValues[middle]) / 2d
            : orderedValues[middle];
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Suggestion timestamps must be expressed in UTC.", parameterName);
        }
    }
}

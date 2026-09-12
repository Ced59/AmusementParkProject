namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportRideOutcomeStatistics(
    long RecordedOutcomeCount,
    long CompletedRideCount,
    long AttemptedCount,
    long MissedClosedCount,
    long MissedUnavailableCount,
    long SkippedByChoiceCount);

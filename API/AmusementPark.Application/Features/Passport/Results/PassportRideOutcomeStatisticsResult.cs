namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportRideOutcomeStatisticsResult(
    long RecordedOutcomeCount,
    long CompletedRideCount,
    long AttemptedCount,
    long MissedClosedCount,
    long MissedUnavailableCount,
    long SkippedByChoiceCount);

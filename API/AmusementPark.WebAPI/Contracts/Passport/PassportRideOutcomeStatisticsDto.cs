namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRideOutcomeStatisticsDto
{
    public long RecordedOutcomeCount { get; init; }
    public long CompletedRideCount { get; init; }
    public long AttemptedCount { get; init; }
    public long MissedClosedCount { get; init; }
    public long MissedUnavailableCount { get; init; }
    public long SkippedByChoiceCount { get; init; }
}

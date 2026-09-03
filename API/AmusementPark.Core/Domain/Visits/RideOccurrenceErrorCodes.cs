namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Codes métier stables associés à une occurrence de ride.
/// </summary>
public static class RideOccurrenceErrorCodes
{
    public const string InvalidStatus = "ride-occurrence.invalid-status";

    public const string InvalidSource = "ride-occurrence.invalid-source";

    public const string InvalidHistoricalConsistency = "ride-occurrence.invalid-historical-consistency";

    public const string InvalidVersion = "ride-occurrence.invalid-version";

    public const string TimestampNotUtc = "ride-occurrence.timestamp-not-utc";

    public const string InvalidTimestampOrder = "ride-occurrence.invalid-timestamp-order";

    public const string TimeRequiresExactDayAndTimeZone = "ride-occurrence.time-requires-exact-day-and-time-zone";

    public const string VisitScopeMismatch = "ride-occurrence.visit-scope-mismatch";

    public const string PrivateNoteTooLong = "ride-occurrence.private-note-too-long";

    public const string AlreadyDeleted = "ride-occurrence.already-deleted";

    public const string DeletedOccurrenceMutation = "ride-occurrence.deleted-occurrence-mutation";

    public const string HistoricalTargetNameRequired = "ride-occurrence.historical-target-name-required";

    public const string HistoricalTargetTextTooLong = "ride-occurrence.historical-target-text-too-long";

    public const string HistoricalTargetControlCharacter = "ride-occurrence.historical-target-control-character";
}

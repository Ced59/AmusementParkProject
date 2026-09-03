namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Codes métier stables associés à l'agrégat visite.
/// </summary>
public static class VisitErrorCodes
{
    public const string InvalidStatus = "visit.invalid-status";

    public const string InvalidPrivacy = "visit.invalid-privacy";

    public const string InvalidServiceDayConvention = "visit.invalid-service-day-convention";

    public const string InvalidVersion = "visit.invalid-version";

    public const string TimestampNotUtc = "visit.timestamp-not-utc";

    public const string InvalidTimestampOrder = "visit.invalid-timestamp-order";

    public const string InvalidTransition = "visit.invalid-transition";

    public const string FutureCompletedDate = "visit.future-completed-date";

    public const string TitleTooLong = "visit.title-too-long";

    public const string TitleControlCharacter = "visit.title-control-character";

    public const string PrivateNoteTooLong = "visit.private-note-too-long";

    public const string TimeZoneIdTooLong = "visit.time-zone-id-too-long";

    public const string TimeZoneIdControlCharacter = "visit.time-zone-id-control-character";

    public const string CompletedAtRequired = "visit.completed-at-required";

    public const string CompletedAtForbidden = "visit.completed-at-forbidden";
}

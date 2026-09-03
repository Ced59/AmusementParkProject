using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Une expérience déclarée sur un élément pendant une visite privée.
/// Chaque tour réel conserve sa propre identité.
/// </summary>
public sealed class RideOccurrence
{
    public const int MaximumPrivateNoteLength = 4000;

    public const long SortPositionStep = 1024;

    private RideOccurrence(
        RideOccurrenceId id,
        VisitId visitId,
        string userId,
        string parkId,
        string parkItemId,
        long sortPosition,
        OccurrenceMoment moment,
        RideOccurrenceStatus status,
        RideLogSource source,
        HistoricalConsistency historicalConsistency,
        HistoricalTargetReference? historicalTarget,
        string? privateNote,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        _ = id.Value;
        _ = visitId.Value;
        ArgumentNullException.ThrowIfNull(moment);
        ValidateStatus(status);
        ValidateSource(source);
        ValidateHistoricalConsistency(historicalConsistency);
        ValidateVersion(version);
        ValidateTimestamps(createdAtUtc, updatedAtUtc, deletedAtUtc);

        this.Id = id;
        this.VisitId = visitId;
        this.UserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        this.ParkId = IdentifierRules.NormalizeRequired(parkId, nameof(parkId));
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.SortPosition = sortPosition;
        this.Moment = moment;
        this.Status = status;
        this.Source = source;
        this.HistoricalConsistency = historicalConsistency;
        this.HistoricalTarget = historicalTarget;
        this.PrivateNote = NormalizePrivateNote(privateNote);
        this.Version = version;
        this.CreatedAtUtc = createdAtUtc;
        this.UpdatedAtUtc = updatedAtUtc;
        this.DeletedAtUtc = deletedAtUtc;
    }

    public RideOccurrenceId Id { get; }

    public VisitId VisitId { get; }

    public string UserId { get; }

    public string ParkId { get; }

    public string ParkItemId { get; }

    public long SortPosition { get; private set; }

    public OccurrenceMoment Moment { get; private set; }

    public RideOccurrenceStatus Status { get; private set; }

    public RideLogSource Source { get; }

    public HistoricalConsistency HistoricalConsistency { get; private set; }

    public HistoricalTargetReference? HistoricalTarget { get; private set; }

    public string? PrivateNote { get; private set; }

    public long Version { get; private set; }

    public DateTime CreatedAtUtc { get; }

    public DateTime UpdatedAtUtc { get; private set; }

    public DateTime? DeletedAtUtc { get; private set; }

    public bool CountsAsRide => this.Status == RideOccurrenceStatus.Completed;

    public bool IsDeleted => this.DeletedAtUtc.HasValue;

    public static RideOccurrence Create(
        RideOccurrenceId id,
        Visit visit,
        string parkItemId,
        long sortPosition,
        OccurrenceMoment moment,
        RideOccurrenceStatus status,
        RideLogSource source,
        HistoricalConsistency historicalConsistency,
        HistoricalTargetReference? historicalTarget,
        string? privateNote,
        DateTime nowUtc)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ValidateMomentAgainstVisit(moment, visit);
        return new RideOccurrence(
            id,
            visit.Id,
            visit.UserId,
            visit.ParkId,
            parkItemId,
            sortPosition,
            moment,
            status,
            source,
            historicalConsistency,
            historicalTarget,
            privateNote,
            1,
            nowUtc,
            nowUtc,
            null);
    }

    public static RideOccurrence Restore(
        RideOccurrenceId id,
        VisitId visitId,
        string userId,
        string parkId,
        string parkItemId,
        long sortPosition,
        OccurrenceMoment moment,
        RideOccurrenceStatus status,
        RideLogSource source,
        HistoricalConsistency historicalConsistency,
        HistoricalTargetReference? historicalTarget,
        string? privateNote,
        long version,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        return new RideOccurrence(
            id,
            visitId,
            userId,
            parkId,
            parkItemId,
            sortPosition,
            moment,
            status,
            source,
            historicalConsistency,
            historicalTarget,
            privateNote,
            version,
            createdAtUtc,
            updatedAtUtc,
            deletedAtUtc);
    }

    public void Update(
        Visit visit,
        OccurrenceMoment moment,
        RideOccurrenceStatus status,
        HistoricalConsistency historicalConsistency,
        HistoricalTargetReference? historicalTarget,
        string? privateNote,
        DateTime nowUtc)
    {
        this.EnsureMutable();
        this.EnsureVisitScope(visit);
        ValidateMomentAgainstVisit(moment, visit);
        ValidateStatus(status);
        ValidateHistoricalConsistency(historicalConsistency);
        string? normalizedPrivateNote = NormalizePrivateNote(privateNote);
        this.ValidateMutationTimestamp(nowUtc);

        if (this.Moment == moment
            && this.Status == status
            && this.HistoricalConsistency == historicalConsistency
            && this.HistoricalTarget == historicalTarget
            && string.Equals(this.PrivateNote, normalizedPrivateNote, StringComparison.Ordinal))
        {
            return;
        }

        this.PrepareMutation(nowUtc);
        this.Moment = moment;
        this.Status = status;
        this.HistoricalConsistency = historicalConsistency;
        this.HistoricalTarget = historicalTarget;
        this.PrivateNote = normalizedPrivateNote;
        this.CommitMutation(nowUtc);
    }

    public void MoveTo(long sortPosition, DateTime nowUtc)
    {
        this.EnsureMutable();
        this.ValidateMutationTimestamp(nowUtc);
        if (this.SortPosition == sortPosition)
        {
            return;
        }

        this.PrepareMutation(nowUtc);
        this.SortPosition = sortPosition;
        this.CommitMutation(nowUtc);
    }

    public void Delete(DateTime nowUtc)
    {
        if (this.IsDeleted)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.AlreadyDeleted,
                "A deleted ride occurrence cannot be deleted again.");
        }

        this.PrepareMutation(nowUtc);
        this.DeletedAtUtc = nowUtc;
        this.CommitMutation(nowUtc);
    }

    private static void ValidateMomentAgainstVisit(OccurrenceMoment moment, Visit visit)
    {
        ArgumentNullException.ThrowIfNull(moment);
        if (moment.LocalTime.HasValue
            && (visit.Date.Precision != VisitDatePrecision.Day
                || string.IsNullOrWhiteSpace(visit.TimeZoneId)))
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.TimeRequiresExactDayAndTimeZone,
                "A local ride time requires an exact visit day and a time zone.");
        }
    }

    private static void ValidateStatus(RideOccurrenceStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidStatus,
                "The ride occurrence status is invalid.");
        }
    }

    private static void ValidateSource(RideLogSource source)
    {
        if (!Enum.IsDefined(source))
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidSource,
                "The ride log source is invalid.");
        }
    }

    private static void ValidateHistoricalConsistency(HistoricalConsistency historicalConsistency)
    {
        if (!Enum.IsDefined(historicalConsistency))
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidHistoricalConsistency,
                "The historical consistency value is invalid.");
        }
    }

    private static void ValidateVersion(long version)
    {
        if (version < 1)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidVersion,
                "The ride occurrence version must be positive.");
        }
    }

    private static void ValidateTimestamps(
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime? deletedAtUtc)
    {
        EnsureUtc(createdAtUtc);
        EnsureUtc(updatedAtUtc);
        if (deletedAtUtc.HasValue)
        {
            EnsureUtc(deletedAtUtc.Value);
        }

        if (updatedAtUtc < createdAtUtc
            || (deletedAtUtc.HasValue
                && (deletedAtUtc.Value < createdAtUtc || deletedAtUtc.Value > updatedAtUtc)))
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidTimestampOrder,
                "Ride occurrence timestamps are not chronologically consistent.");
        }
    }

    private static void EnsureUtc(DateTime timestamp)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.TimestampNotUtc,
                "Ride occurrence timestamps must be expressed in UTC.");
        }
    }

    private static string? NormalizePrivateNote(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            return null;
        }

        if (normalizedValue.Length > MaximumPrivateNoteLength)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.PrivateNoteTooLong,
                $"The private ride note cannot exceed {MaximumPrivateNoteLength} characters.");
        }

        return normalizedValue;
    }

    private void EnsureVisitScope(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (this.VisitId != visit.Id
            || !string.Equals(this.UserId, visit.UserId, StringComparison.Ordinal)
            || !string.Equals(this.ParkId, visit.ParkId, StringComparison.Ordinal))
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.VisitScopeMismatch,
                "The visit does not own this ride occurrence.");
        }
    }

    private void EnsureMutable()
    {
        if (this.IsDeleted)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.DeletedOccurrenceMutation,
                "A deleted ride occurrence cannot be changed.");
        }
    }

    private void PrepareMutation(DateTime nowUtc)
    {
        this.ValidateMutationTimestamp(nowUtc);
        if (this.Version == long.MaxValue)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidVersion,
                "The ride occurrence version cannot be incremented further.");
        }
    }

    private void ValidateMutationTimestamp(DateTime nowUtc)
    {
        EnsureUtc(nowUtc);
        if (nowUtc < this.UpdatedAtUtc)
        {
            throw CreateValidationException(
                RideOccurrenceErrorCodes.InvalidTimestampOrder,
                "A ride occurrence mutation cannot predate the current state.");
        }
    }

    private void CommitMutation(DateTime nowUtc)
    {
        this.Version++;
        this.UpdatedAtUtc = nowUtc;
    }

    private static RideOccurrenceValidationException CreateValidationException(
        string errorCode,
        string message)
    {
        return new RideOccurrenceValidationException(errorCode, message);
    }
}

using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed record RideOccurrenceAuditSnapshot(
    RideOccurrenceStatus Status,
    OccurrenceMoment Moment,
    HistoricalConsistency HistoricalConsistency,
    HistoricalTargetReference? HistoricalTarget,
    string? PrivateNote,
    long SortPosition,
    byte? AssessmentValueHalfSteps,
    string? AssessmentPrivateComment,
    int? AssessmentRevision)
{
    public static RideOccurrenceAuditSnapshot Capture(RideOccurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return new RideOccurrenceAuditSnapshot(
            occurrence.Status,
            occurrence.Moment,
            occurrence.HistoricalConsistency,
            occurrence.HistoricalTarget,
            occurrence.PrivateNote,
            occurrence.SortPosition,
            occurrence.Assessment?.Value.HalfSteps,
            occurrence.Assessment?.PrivateComment,
            occurrence.Assessment?.Revision);
    }
}

public sealed record VisitParkAssessmentAuditSnapshot(
    byte ValueHalfSteps,
    string? PrivateComment,
    int Revision)
{
    public static VisitParkAssessmentAuditSnapshot? Capture(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return visit.ParkAssessment is null
            ? null
            : new VisitParkAssessmentAuditSnapshot(
                visit.ParkAssessment.Value.HalfSteps,
                visit.ParkAssessment.PrivateComment,
                visit.ParkAssessment.Revision);
    }
}

public sealed record VisitAuditSnapshot(
    VisitDate Date,
    string? TimeZoneId,
    LocalServiceDayConvention ServiceDayConvention,
    VisitStatus Status,
    string? Title,
    string? PrivateNote)
{
    public static VisitAuditSnapshot Capture(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return new VisitAuditSnapshot(
            visit.Date,
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            visit.Status,
            visit.Title,
            visit.PrivateNote);
    }
}

/// <summary>
/// Traduit les mutations validées des visites en preuves minimisées, sans texte privé.
/// </summary>
public static class PassportVisitAuditEventFactory
{
    public static PassportAuditEvent VisitCreated(Visit visit, string correlationSeed)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return PassportAuditEvent.Create(
            visit.UserId,
            PassportAuditEntityType.Visit,
            visit.Id.Value,
            visit.Id.Value,
            visit.ParkId,
            null,
            PassportAuditEventType.VisitCreated,
            visit.Version,
            null,
            new[] { PassportAuditChangedField.Visit },
            null,
            null,
            null,
            visit.Date,
            null,
            visit.Status,
            null,
            null,
            null,
            null,
            !string.IsNullOrWhiteSpace(visit.Title)
                || !string.IsNullOrWhiteSpace(visit.PrivateNote),
            correlationSeed,
            PassportAuditOrigin.User,
            visit.CreatedAtUtc);
    }

    public static PassportAuditEvent VisitUpdated(
        Visit visit,
        VisitAuditSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(previous);
        List<PassportAuditChangedField> fields = new List<PassportAuditChangedField>();
        bool dateChanged = previous.Date != visit.Date;
        if (dateChanged)
        {
            fields.Add(PassportAuditChangedField.Date);
        }

        if (!string.Equals(previous.TimeZoneId, visit.TimeZoneId, StringComparison.Ordinal))
        {
            fields.Add(PassportAuditChangedField.TimeZone);
        }

        if (previous.ServiceDayConvention != visit.ServiceDayConvention)
        {
            fields.Add(PassportAuditChangedField.ServiceDayConvention);
        }

        bool titleChanged = !string.Equals(previous.Title, visit.Title, StringComparison.Ordinal);
        if (titleChanged)
        {
            fields.Add(PassportAuditChangedField.Title);
        }

        bool privateNoteChanged = !string.Equals(
            previous.PrivateNote,
            visit.PrivateNote,
            StringComparison.Ordinal);
        if (privateNoteChanged)
        {
            fields.Add(PassportAuditChangedField.PrivateNote);
        }

        return PassportAuditEvent.Create(
            visit.UserId,
            PassportAuditEntityType.Visit,
            visit.Id.Value,
            visit.Id.Value,
            visit.ParkId,
            null,
            dateChanged
                ? PassportAuditEventType.VisitDateChanged
                : PassportAuditEventType.VisitMetadataChanged,
            visit.Version,
            null,
            fields,
            null,
            null,
            dateChanged ? previous.Date : null,
            dateChanged ? visit.Date : null,
            previous.Status,
            visit.Status,
            null,
            null,
            null,
            null,
            titleChanged || privateNoteChanged,
            $"{visit.Id.Value}:{visit.Version}:visit-update",
            PassportAuditOrigin.User,
            visit.UpdatedAtUtc);
    }

    public static PassportAuditEvent VisitStatusChanged(
        Visit visit,
        VisitStatus previousStatus)
    {
        ArgumentNullException.ThrowIfNull(visit);
        PassportAuditEventType eventType = visit.Status switch
        {
            VisitStatus.Completed => PassportAuditEventType.VisitCompleted,
            VisitStatus.Draft => PassportAuditEventType.VisitReopened,
            VisitStatus.Archived => PassportAuditEventType.VisitArchived,
            _ => throw new ArgumentException("The visit status transition is not auditable.", nameof(visit)),
        };
        return PassportAuditEvent.Create(
            visit.UserId,
            PassportAuditEntityType.Visit,
            visit.Id.Value,
            visit.Id.Value,
            visit.ParkId,
            null,
            eventType,
            visit.Version,
            null,
            new[] { PassportAuditChangedField.Status },
            null,
            null,
            null,
            null,
            previousStatus,
            visit.Status,
            null,
            null,
            null,
            null,
            false,
            $"{visit.Id.Value}:{visit.Version}:visit-status",
            PassportAuditOrigin.User,
            visit.UpdatedAtUtc);
    }

    public static PassportAuditEvent ParkAssessmentUpserted(
        Visit visit,
        VisitParkAssessmentAuditSnapshot? previous)
    {
        ArgumentNullException.ThrowIfNull(visit);
        VisitParkAssessment current = visit.ParkAssessment
            ?? throw new ArgumentException("The visit must contain an assessment.", nameof(visit));
        bool ratingChanged = previous is null
            || previous.ValueHalfSteps != current.Value.HalfSteps;
        bool commentChanged = previous is null
            ? current.PrivateComment is not null
            : !string.Equals(
                previous.PrivateComment,
                current.PrivateComment,
                StringComparison.Ordinal);
        List<PassportAuditChangedField> fields = new List<PassportAuditChangedField>();
        if (ratingChanged)
        {
            fields.Add(PassportAuditChangedField.ParkAssessmentRating);
        }

        if (commentChanged)
        {
            fields.Add(PassportAuditChangedField.ParkAssessmentPrivateComment);
        }

        fields.Add(PassportAuditChangedField.AssessmentRevision);

        return PassportAuditEvent.Create(
            visit.UserId,
            PassportAuditEntityType.ParkAssessment,
            visit.Id.Value,
            visit.Id.Value,
            visit.ParkId,
            null,
            previous is null
                ? PassportAuditEventType.ParkAssessmentCreated
                : PassportAuditEventType.ParkAssessmentChanged,
            visit.Version,
            current.Revision,
            fields,
            previous?.ValueHalfSteps,
            current.Value.HalfSteps,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            commentChanged,
            $"{visit.Id.Value}:{current.Revision}:park-assessment",
            PassportAuditOrigin.User,
            current.UpdatedAtUtc);
    }

    public static PassportAuditEvent ParkAssessmentDeleted(
        Visit visit,
        VisitParkAssessmentAuditSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(visit);
        ArgumentNullException.ThrowIfNull(previous);
        List<PassportAuditChangedField> fields = new List<PassportAuditChangedField>
        {
            PassportAuditChangedField.ParkAssessmentRating,
            PassportAuditChangedField.AssessmentRevision,
        };
        if (previous.PrivateComment is not null)
        {
            fields.Add(PassportAuditChangedField.ParkAssessmentPrivateComment);
        }

        return PassportAuditEvent.Create(
            visit.UserId,
            PassportAuditEntityType.ParkAssessment,
            visit.Id.Value,
            visit.Id.Value,
            visit.ParkId,
            null,
            PassportAuditEventType.ParkAssessmentDeleted,
            visit.Version,
            previous.Revision + 1,
            fields,
            previous.ValueHalfSteps,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            previous.PrivateComment is not null,
            $"{visit.Id.Value}:{visit.Version}:park-assessment-delete",
            PassportAuditOrigin.User,
            visit.UpdatedAtUtc);
    }
}

/// <summary>
/// Traduit les mutations validées des occurrences en preuves minimisées.
/// </summary>
public static class PassportRideAuditEventFactory
{
    public static PassportAuditEvent RideOccurrenceAdded(
        RideOccurrence occurrence,
        string correlationSeed)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return PassportAuditEvent.Create(
            occurrence.UserId,
            PassportAuditEntityType.RideOccurrence,
            occurrence.Id.Value,
            occurrence.VisitId.Value,
            occurrence.ParkId,
            occurrence.ParkItemId,
            PassportAuditEventType.RideOccurrenceAdded,
            occurrence.Version,
            null,
            new[] { PassportAuditChangedField.RideOccurrence },
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            occurrence.Status,
            null,
            occurrence.SortPosition,
            occurrence.PrivateNote is not null,
            correlationSeed,
            PassportAuditOrigin.User,
            occurrence.CreatedAtUtc);
    }

    public static PassportAuditEvent RideOccurrenceChanged(
        RideOccurrence occurrence,
        RideOccurrenceAuditSnapshot previous,
        string correlationSeed)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(previous);
        List<PassportAuditChangedField> fields = BuildRideOccurrenceChangedFields(
            occurrence,
            previous);
        return PassportAuditEvent.Create(
            occurrence.UserId,
            PassportAuditEntityType.RideOccurrence,
            occurrence.Id.Value,
            occurrence.VisitId.Value,
            occurrence.ParkId,
            occurrence.ParkItemId,
            PassportAuditEventType.RideOccurrenceChanged,
            occurrence.Version,
            null,
            fields,
            null,
            null,
            null,
            null,
            null,
            null,
            previous.Status,
            occurrence.Status,
            previous.SortPosition,
            occurrence.SortPosition,
            !string.Equals(previous.PrivateNote, occurrence.PrivateNote, StringComparison.Ordinal),
            correlationSeed,
            PassportAuditOrigin.User,
            occurrence.UpdatedAtUtc);
    }

    public static PassportAuditEvent RideOccurrenceDeleted(
        RideOccurrence occurrence,
        string correlationSeed)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        if (!occurrence.IsDeleted)
        {
            throw new ArgumentException("The occurrence must be deleted.", nameof(occurrence));
        }

        return PassportAuditEvent.Create(
            occurrence.UserId,
            PassportAuditEntityType.RideOccurrence,
            occurrence.Id.Value,
            occurrence.VisitId.Value,
            occurrence.ParkId,
            occurrence.ParkItemId,
            PassportAuditEventType.RideOccurrenceDeleted,
            occurrence.Version,
            null,
            new[] { PassportAuditChangedField.DeletedAtUtc },
            null,
            null,
            null,
            null,
            null,
            null,
            occurrence.Status,
            occurrence.Status,
            occurrence.SortPosition,
            occurrence.SortPosition,
            false,
            correlationSeed,
            PassportAuditOrigin.User,
            occurrence.DeletedAtUtc!.Value);
    }

    public static PassportAuditEvent RideAssessmentUpserted(
        RideOccurrence occurrence,
        RideOccurrenceAuditSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(previous);
        RideAssessment current = occurrence.Assessment
            ?? throw new ArgumentException("The occurrence must contain an assessment.", nameof(occurrence));
        bool ratingChanged = previous.AssessmentValueHalfSteps != current.Value.HalfSteps;
        bool commentChanged = !string.Equals(
            previous.AssessmentPrivateComment,
            current.PrivateComment,
            StringComparison.Ordinal);
        List<PassportAuditChangedField> fields = new List<PassportAuditChangedField>();
        if (ratingChanged)
        {
            fields.Add(PassportAuditChangedField.RideAssessmentRating);
        }

        if (commentChanged)
        {
            fields.Add(PassportAuditChangedField.RideAssessmentPrivateComment);
        }

        fields.Add(PassportAuditChangedField.AssessmentRevision);

        return PassportAuditEvent.Create(
            occurrence.UserId,
            PassportAuditEntityType.RideAssessment,
            occurrence.Id.Value,
            occurrence.VisitId.Value,
            occurrence.ParkId,
            occurrence.ParkItemId,
            previous.AssessmentRevision.HasValue
                ? PassportAuditEventType.RideAssessmentChanged
                : PassportAuditEventType.RideAssessmentCreated,
            occurrence.Version,
            current.Revision,
            fields,
            previous.AssessmentValueHalfSteps,
            current.Value.HalfSteps,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            commentChanged,
            $"{occurrence.Id.Value}:{current.Revision}:ride-assessment",
            PassportAuditOrigin.User,
            current.UpdatedAtUtc);
    }

    public static PassportAuditEvent RideAssessmentDeleted(
        RideOccurrence occurrence,
        RideOccurrenceAuditSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        ArgumentNullException.ThrowIfNull(previous);
        if (!previous.AssessmentRevision.HasValue
            || !previous.AssessmentValueHalfSteps.HasValue)
        {
            throw new ArgumentException("A previous assessment is required.", nameof(previous));
        }

        List<PassportAuditChangedField> fields = new List<PassportAuditChangedField>
        {
            PassportAuditChangedField.RideAssessmentRating,
            PassportAuditChangedField.AssessmentRevision,
        };
        if (previous.AssessmentPrivateComment is not null)
        {
            fields.Add(PassportAuditChangedField.RideAssessmentPrivateComment);
        }

        return PassportAuditEvent.Create(
            occurrence.UserId,
            PassportAuditEntityType.RideAssessment,
            occurrence.Id.Value,
            occurrence.VisitId.Value,
            occurrence.ParkId,
            occurrence.ParkItemId,
            PassportAuditEventType.RideAssessmentDeleted,
            occurrence.Version,
            previous.AssessmentRevision.Value + 1,
            fields,
            previous.AssessmentValueHalfSteps,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            previous.AssessmentPrivateComment is not null,
            $"{occurrence.Id.Value}:{occurrence.Version}:ride-assessment-delete",
            PassportAuditOrigin.User,
            occurrence.UpdatedAtUtc);
    }

    private static List<PassportAuditChangedField> BuildRideOccurrenceChangedFields(
        RideOccurrence occurrence,
        RideOccurrenceAuditSnapshot previous)
    {
        List<PassportAuditChangedField> fields = new List<PassportAuditChangedField>();
        if (occurrence.Status != previous.Status)
        {
            fields.Add(PassportAuditChangedField.Status);
        }

        if (occurrence.Moment != previous.Moment)
        {
            fields.Add(PassportAuditChangedField.Moment);
        }

        if (occurrence.HistoricalConsistency != previous.HistoricalConsistency)
        {
            fields.Add(PassportAuditChangedField.HistoricalConsistency);
        }

        if (occurrence.HistoricalTarget != previous.HistoricalTarget)
        {
            fields.Add(PassportAuditChangedField.HistoricalTarget);
        }

        if (!string.Equals(previous.PrivateNote, occurrence.PrivateNote, StringComparison.Ordinal))
        {
            fields.Add(PassportAuditChangedField.PrivateNote);
        }

        if (occurrence.SortPosition != previous.SortPosition)
        {
            fields.Add(PassportAuditChangedField.SortPosition);
        }

        return fields;
    }
}

using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

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

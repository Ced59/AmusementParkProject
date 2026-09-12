using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

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

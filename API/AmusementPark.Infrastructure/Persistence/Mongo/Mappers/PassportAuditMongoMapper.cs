using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class PassportAuditMongoMapper
{
    public static PassportAuditEventDocument ToDocument(this PassportAuditEvent auditEvent)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        return new PassportAuditEventDocument
        {
            EventId = auditEvent.Id,
            UserId = auditEvent.UserId,
            EntityType = auditEvent.EntityType,
            EntityId = auditEvent.EntityId,
            VisitId = auditEvent.VisitId,
            ParkId = auditEvent.ParkId,
            ParkItemId = auditEvent.ParkItemId,
            EventType = auditEvent.EventType,
            EntityVersion = auditEvent.EntityVersion,
            AssessmentRevision = auditEvent.AssessmentRevision,
            ChangedFields = auditEvent.ChangedFields
                .Select(static field => field.ToString())
                .ToList(),
            PreviousRatingHalfSteps = auditEvent.PreviousRatingHalfSteps,
            NewRatingHalfSteps = auditEvent.NewRatingHalfSteps,
            PreviousVisitDate = auditEvent.PreviousVisitDate?.ToDocument(),
            NewVisitDate = auditEvent.NewVisitDate?.ToDocument(),
            PreviousVisitStatus = auditEvent.PreviousVisitStatus,
            NewVisitStatus = auditEvent.NewVisitStatus,
            PreviousRideStatus = auditEvent.PreviousRideStatus,
            NewRideStatus = auditEvent.NewRideStatus,
            PreviousSortPosition = auditEvent.PreviousSortPosition,
            NewSortPosition = auditEvent.NewSortPosition,
            PrivateTextChanged = auditEvent.PrivateTextChanged,
            CorrelationId = auditEvent.CorrelationId,
            Origin = auditEvent.Origin,
            OccurredAtUtc = auditEvent.OccurredAtUtc,
        };
    }

    public static PassportAuditEvent ToDomain(this PassportAuditEventDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return PassportAuditEvent.Restore(
            document.EventId,
            document.UserId,
            document.EntityType,
            document.EntityId,
            document.VisitId,
            document.ParkId,
            document.ParkItemId,
            document.EventType,
            document.EntityVersion,
            document.AssessmentRevision,
            document.ChangedFields
                .Select(static field => Enum.Parse<PassportAuditChangedField>(field))
                .ToArray(),
            document.PreviousRatingHalfSteps,
            document.NewRatingHalfSteps,
            document.PreviousVisitDate?.ToDomain(),
            document.NewVisitDate?.ToDomain(),
            document.PreviousVisitStatus,
            document.NewVisitStatus,
            document.PreviousRideStatus,
            document.NewRideStatus,
            document.PreviousSortPosition,
            document.NewSortPosition,
            document.PrivateTextChanged,
            document.CorrelationId,
            document.Origin,
            document.OccurredAtUtc);
    }

    private static VisitDateDocument ToDocument(this VisitDate date)
    {
        return new VisitDateDocument
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            Precision = date.Precision,
            IsApproximate = date.IsApproximate,
        };
    }

    private static VisitDate ToDomain(this VisitDateDocument document)
    {
        return new VisitDate(
            document.Year,
            document.Month,
            document.Day,
            document.Precision,
            document.IsApproximate);
    }
}

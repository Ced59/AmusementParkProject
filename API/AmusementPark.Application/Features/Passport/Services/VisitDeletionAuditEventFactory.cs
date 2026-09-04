using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public static class VisitDeletionAuditEventFactory
{
    public static PassportAuditEvent Create(Visit visit, DateTime deletedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(visit);
        return PassportAuditEvent.Create(
            visit.UserId,
            PassportAuditEntityType.Visit,
            visit.Id.Value,
            visit.Id.Value,
            visit.ParkId,
            null,
            PassportAuditEventType.VisitDeleted,
            visit.Version + 1,
            null,
            new[] { PassportAuditChangedField.DeletedAtUtc },
            null,
            null,
            null,
            null,
            visit.Status,
            null,
            null,
            null,
            null,
            null,
            !string.IsNullOrWhiteSpace(visit.Title)
                || !string.IsNullOrWhiteSpace(visit.PrivateNote)
                || !string.IsNullOrWhiteSpace(visit.ParkAssessment?.PrivateComment),
            $"{visit.Id.Value}:{visit.Version + 1}:visit-delete",
            PassportAuditOrigin.User,
            deletedAtUtc);
    }
}

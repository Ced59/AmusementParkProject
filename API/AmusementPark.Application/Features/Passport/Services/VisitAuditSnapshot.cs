using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

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

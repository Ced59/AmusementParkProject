using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

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

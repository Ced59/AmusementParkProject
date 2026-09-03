using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportVisitResultFactory
{
    public static VisitResult Create(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        return new VisitResult(
            visit.Id.Value,
            visit.ParkId,
            new VisitDateResult(
                visit.Date.Year,
                visit.Date.Month,
                visit.Date.Day,
                visit.Date.Precision,
                visit.Date.IsApproximate),
            visit.TimeZoneId,
            visit.ServiceDayConvention,
            visit.Status,
            visit.Privacy,
            visit.Title,
            visit.PrivateNote,
            visit.Version,
            visit.CreatedAtUtc,
            visit.UpdatedAtUtc,
            visit.CompletedAtUtc,
            visit.ParkAssessment is null
                ? null
                : new VisitParkAssessmentResult(
                    visit.ParkAssessment.Value.DoubleValue,
                    visit.ParkAssessment.PrivateComment,
                    visit.ParkAssessment.Revision,
                    visit.ParkAssessment.CreatedAtUtc,
                    visit.ParkAssessment.UpdatedAtUtc));
    }
}

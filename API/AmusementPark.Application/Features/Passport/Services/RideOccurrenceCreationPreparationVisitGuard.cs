using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class RideOccurrenceCreationPreparationVisitGuard
{
    public static bool Matches(
        RideOccurrenceCreationPreparation preparation,
        Visit visit)
    {
        ArgumentNullException.ThrowIfNull(preparation);
        ArgumentNullException.ThrowIfNull(visit);
        return string.Equals(
                preparation.ParkId,
                visit.ParkId,
                StringComparison.Ordinal)
            && preparation.VisitDate == visit.Date
            && string.Equals(
                preparation.TimeZoneId,
                visit.TimeZoneId,
                StringComparison.Ordinal)
            && preparation.ServiceDayConvention == visit.ServiceDayConvention;
    }
}

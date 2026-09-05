using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportVisitRideTargetEvaluationHttpMapper
{
    public static PassportVisitRideTargetEvaluationDto ToHttp(
        this VisitRideTargetEvaluationResult result)
    {
        return new PassportVisitRideTargetEvaluationDto
        {
            ParkItemId = result.ParkItemId,
            HistoricalConsistency =
                (PassportHistoricalConsistencyDto)result.HistoricalConsistency,
            OpeningDate = result.OpeningDate,
            ClosingDate = result.ClosingDate,
        };
    }
}

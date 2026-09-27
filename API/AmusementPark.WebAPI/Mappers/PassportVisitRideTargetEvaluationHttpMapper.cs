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
            Name = result.Name,
            Category = result.Category,
            OperationalState = result.OperationalState.ToString(),
            HistoricalConsistency =
                (PassportHistoricalConsistencyDto)result.HistoricalConsistency,
            IsHistoricalOnly = result.IsHistoricalOnly,
            MainImageId = result.MainImageId,
            ZoneId = result.ZoneId,
            LifecycleStatus = result.LifecycleStatus,
            OpeningDate = result.OpeningDate,
            ClosingDate = result.ClosingDate,
        };
    }
}

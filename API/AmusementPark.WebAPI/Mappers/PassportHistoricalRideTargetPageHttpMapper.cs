using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportHistoricalRideTargetPageHttpMapper
{
    public static PassportHistoricalRideTargetPageDto ToHttp(
        this VisitHistoricalRideTargetPageResult result)
    {
        return new PassportHistoricalRideTargetPageDto
        {
            Items = result.Items.Select(static item => item.ToHttp()).ToArray(),
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.TotalPages,
            KnownOpenCount = result.KnownOpenCount,
            PossiblyOpenCount = result.PossiblyOpenCount,
            AllHistoryCount = result.AllHistoryCount,
            CoverageStatus = result.CoverageStatus.ToString(),
            CoveragePercent = result.CoveragePercent,
            MethodologyVersion = result.MethodologyVersion,
        };
    }
}

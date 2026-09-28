using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.WebAPI.Contracts.Passport;

namespace AmusementPark.WebAPI.Mappers;

internal static class PassportHistoricalStatisticsHttpMapper
{
    public static PassportHistoricalStatisticsDto ToHttp(
        this PassportHistoricalStatisticsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new PassportHistoricalStatisticsDto
        {
            VisitCount = result.VisitCount,
            FirstVisitYear = result.FirstVisitYear,
            CompletedRideCount = result.CompletedRideCount,
            CanonicallyResolvedRideCount = result.CanonicallyResolvedRideCount,
            CanonicalCoverageRate = result.CanonicalCoverageRate,
            ParkCountAcrossMultipleEras = result.ParkCountAcrossMultipleEras,
            ParksAcrossEras = result.ParksAcrossEras.Select(static item =>
                new PassportHistoricalParkEraDto
                {
                    ParkName = item.ParkName,
                    FirstVisitYear = item.FirstVisitYear,
                    LastVisitYear = item.LastVisitYear,
                    VisitCount = item.VisitCount,
                    CanonicalEraCount = item.CanonicalEraCount,
                }).ToArray(),
            DisappearedAttractions = result.DisappearedAttractions.Select(static item =>
                new PassportHistoricalDisappearedAttractionDto
                {
                    ParkName = item.ParkName,
                    AttractionName = item.AttractionName,
                    FirstVisitYear = item.FirstVisitYear,
                    LastVisitYear = item.LastVisitYear,
                    CompletedRideCount = item.CompletedRideCount,
                }).ToArray(),
            Transformations = result.Transformations.Select(static item =>
                new PassportHistoricalTransformationDto
                {
                    ParkName = item.ParkName,
                    CurrentName = item.CurrentName,
                    CurrentCategory = item.CurrentCategory,
                    NamesAtVisit = item.NamesAtVisit,
                    CategoriesAtVisit = item.CategoriesAtVisit,
                    FirstVisitYear = item.FirstVisitYear,
                    LastVisitYear = item.LastVisitYear,
                    CompletedRideCount = item.CompletedRideCount,
                }).ToArray(),
            HistoricalNames = result.HistoricalNames.Select(static item =>
                new PassportHistoricalNameDto
                {
                    ParkName = item.ParkName,
                    NameAtVisit = item.NameAtVisit,
                    CurrentName = item.CurrentName,
                    FirstVisitYear = item.FirstVisitYear,
                    LastVisitYear = item.LastVisitYear,
                    CompletedRideCount = item.CompletedRideCount,
                }).ToArray(),
            HistoricalCategories = result.HistoricalCategories.Select(static item =>
                new PassportHistoricalCategoryDto
                {
                    Category = item.Category,
                    CompletedRideCount = item.CompletedRideCount,
                    DistinctAttractionCount = item.DistinctAttractionCount,
                }).ToArray(),
        };
    }
}

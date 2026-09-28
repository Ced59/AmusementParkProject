using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportHistoricalStatisticsResultFactory
{
    public static PassportHistoricalStatisticsResult Create(
        PassportHistoricalStatistics statistics,
        IReadOnlyDictionary<string, string?> parkNames)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(parkNames);
        return new PassportHistoricalStatisticsResult(
            statistics.VisitCount,
            statistics.FirstVisitYear,
            statistics.CompletedRideCount,
            statistics.CanonicallyResolvedRideCount,
            statistics.CanonicalCoverageRate,
            statistics.ParkCountAcrossMultipleEras,
            statistics.ParksAcrossEras.Select(item => new PassportHistoricalParkEraResult(
                parkNames.GetValueOrDefault(item.ParkId),
                item.FirstVisitYear,
                item.LastVisitYear,
                item.VisitCount,
                item.CanonicalEraCount)).ToArray(),
            statistics.DisappearedAttractions.Select(item =>
                new PassportHistoricalDisappearedAttractionResult(
                    parkNames.GetValueOrDefault(item.ParkId),
                    item.NameAtVisit,
                    item.FirstVisitYear,
                    item.LastVisitYear,
                    item.CompletedRideCount)).ToArray(),
            statistics.Transformations.Select(item =>
                new PassportHistoricalTransformationResult(
                    parkNames.GetValueOrDefault(item.ParkId),
                    item.CurrentName,
                    item.CurrentCategory,
                    item.NamesAtVisit,
                    item.CategoriesAtVisit,
                    item.FirstVisitYear,
                    item.LastVisitYear,
                    item.CompletedRideCount)).ToArray(),
            statistics.HistoricalNames.Select(item => new PassportHistoricalNameResult(
                parkNames.GetValueOrDefault(item.ParkId),
                item.NameAtVisit,
                item.CurrentName,
                item.FirstVisitYear,
                item.LastVisitYear,
                item.CompletedRideCount)).ToArray(),
            statistics.HistoricalCategories.Select(item =>
                new PassportHistoricalCategoryResult(
                    item.Category,
                    item.CompletedRideCount,
                    item.DistinctAttractionCount)).ToArray());
    }
}

using AmusementPark.Application.Features.Passport.Results;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class PassportGlobalStatisticsResultFactory
{
    public static PassportGlobalStatisticsResult Create(
        PassportGlobalStatistics statistics,
        int? selectedYear,
        string? selectedParkId,
        IReadOnlyCollection<int> availableYears,
        IReadOnlyCollection<string> availableParkIds,
        IReadOnlyDictionary<string, string?> parkNames,
        IReadOnlyDictionary<string, string?> parkItemNames)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        ArgumentNullException.ThrowIfNull(availableYears);
        ArgumentNullException.ThrowIfNull(availableParkIds);
        ArgumentNullException.ThrowIfNull(parkNames);
        ArgumentNullException.ThrowIfNull(parkItemNames);

        return new PassportGlobalStatisticsResult(
            selectedYear,
            selectedParkId,
            availableYears
                .Distinct()
                .OrderByDescending(static year => year)
                .ToArray(),
            availableParkIds
                .Distinct(StringComparer.Ordinal)
                .Select(parkId => new PassportGlobalFilterParkResult(
                    parkId,
                    PassportStatisticsResultFactory.ResolveName(parkNames, parkId)))
                .OrderBy(static park => park.ParkName is null)
                .ThenBy(static park => park.ParkName, StringComparer.CurrentCultureIgnoreCase)
                .ToArray(),
            statistics.ParkCount,
            PassportStatisticsResultFactory.ToResult(statistics.Summary),
            statistics.ActivityByYear.Select(static item =>
                new PassportGlobalYearActivityResult(
                    item.Year,
                    item.VisitCount,
                    item.RecordedRideCount)).ToArray(),
            statistics.TopParks.Select(item => new PassportGlobalParkActivityResult(
                item.ParkId,
                PassportStatisticsResultFactory.ResolveName(parkNames, item.ParkId),
                item.VisitCount,
                item.RecordedRideCount)).ToArray(),
            statistics.TopItems.Select(item => new PassportGlobalItemActivityResult(
                item.ParkItemId,
                PassportStatisticsResultFactory.ResolveName(parkItemNames, item.ParkItemId),
                item.ParkId,
                PassportStatisticsResultFactory.ResolveName(parkNames, item.ParkId),
                item.CompletedRideCount)).ToArray(),
            statistics.RatingEvolution.Select(static item =>
                new PassportGlobalRatingEvolutionResult(
                    item.Year,
                    item.ParkAverage,
                    item.RatedVisitCount,
                    item.RideAverage,
                    item.RatedRideCount)).ToArray());
    }
}

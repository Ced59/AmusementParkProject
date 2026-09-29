using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Services;

internal static class StandaloneAttractionWeatherTargetResolver
{
    internal static async Task<IReadOnlyCollection<StandaloneAttraction>> ResolveAsync(
        ParkWeatherRun run,
        IStandaloneAttractionRepository? standaloneAttractionRepository,
        IStandaloneAttractionWeatherRepository? standaloneWeatherRepository,
        IParkWeatherRunRepository runRepository,
        CancellationToken cancellationToken)
    {
        if (standaloneAttractionRepository is null || standaloneWeatherRepository is null)
        {
            return Array.Empty<StandaloneAttraction>();
        }

        if (run.Scope == ParkWeatherRefreshScope.FullVisibleParks)
        {
            IReadOnlyCollection<StandaloneAttraction> visibleAttractions =
                await standaloneAttractionRepository.GetVisibleMapPointsAsync(
                    new StandaloneAttractionSearchCriteria(
                        null,
                        Array.Empty<string>(),
                        Array.Empty<string>()),
                    cancellationToken);
            return FilterEligibleAttractions(visibleAttractions);
        }

        if (run.Scope != ParkWeatherRefreshScope.FailedFromRun)
        {
            return Array.Empty<StandaloneAttraction>();
        }

        IReadOnlyCollection<ParkWeatherRunItem> failedItems = await runRepository.GetRunItemsAsync(
            run.SourceRunId ?? string.Empty,
            ParkWeatherRunItemStatus.Failed,
            cancellationToken);
        IReadOnlyCollection<StandaloneAttraction> attractions =
            await standaloneAttractionRepository.GetByIdsAsync(
                failedItems.Select(static item => item.ParkId).ToList(),
                cancellationToken);
        return FilterEligibleAttractions(attractions);
    }

    private static IReadOnlyCollection<StandaloneAttraction> FilterEligibleAttractions(
        IEnumerable<StandaloneAttraction> attractions)
    {
        return attractions
            .Where(static attraction => attraction.IsVisible)
            .Where(static attraction =>
                attraction.Position is not null
                && (attraction.Position.Latitude != 0d || attraction.Position.Longitude != 0d))
            .Where(static attraction =>
                ParkItemStatusNormalizer.IsOperating(attraction.AttractionDetails?.Status))
            .ToList();
    }
}

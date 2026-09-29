using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionWeatherForecastQueryHandler :
    IQueryHandler<GetStandaloneAttractionWeatherForecastQuery, ApplicationResult<ParkWeatherForecastResult>>
{
    private readonly IStandaloneAttractionRepository attractionRepository;
    private readonly IStandaloneAttractionWeatherRepository weatherRepository;
    private readonly ParkWeatherLocalDateResolver localDateResolver;

    public GetStandaloneAttractionWeatherForecastQueryHandler(
        IStandaloneAttractionRepository attractionRepository,
        IStandaloneAttractionWeatherRepository weatherRepository,
        ParkWeatherLocalDateResolver localDateResolver)
    {
        this.attractionRepository = attractionRepository;
        this.weatherRepository = weatherRepository;
        this.localDateResolver = localDateResolver;
    }

    public async Task<ApplicationResult<ParkWeatherForecastResult>> HandleAsync(
        GetStandaloneAttractionWeatherForecastQuery query,
        CancellationToken cancellationToken = default)
    {
        string attractionId = (query.StandaloneAttractionId ?? string.Empty).Trim();
        StandaloneAttraction? attraction = attractionId.Length == 0
            ? null
            : await this.attractionRepository.GetByIdAsync(
                attractionId,
                includeHidden: false,
                cancellationToken);
        if (attraction is null
            || ParkItemStatusNormalizer.IsClosedDefinitively(attraction.AttractionDetails?.Status))
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        if (attraction.Position is null
            || (attraction.Position.Latitude == 0d && attraction.Position.Longitude == 0d))
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(
                StandaloneAttractionVisitorInformationApplicationErrors.AttractionNotFound());
        }

        int dayCount = Math.Clamp(query.DayCount, 1, 7);
        ParkWeatherDailySnapshot? latest =
            await this.weatherRepository.GetLatestForecastSnapshotAsync(
                attractionId,
                cancellationToken);
        DateOnly localToday = this.localDateResolver.ResolveCurrentLocalDate(latest);
        IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots =
            await this.weatherRepository.GetForecastAsync(
                attractionId,
                localToday,
                dayCount,
                cancellationToken);
        return ApplicationResult<ParkWeatherForecastResult>.Success(
            new ParkWeatherForecastResult
            {
                ParkId = attractionId,
                Days = snapshots
                    .OrderBy(static snapshot => snapshot.LocalDate)
                    .Select(static snapshot => snapshot.ToForecastResult())
                    .ToList(),
            });
    }
}

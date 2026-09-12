using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Queries;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Handlers;

public sealed class GetParkWeatherForecastQueryHandler : IQueryHandler<GetParkWeatherForecastQuery, ApplicationResult<ParkWeatherForecastResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkWeatherRepository weatherRepository;
    private readonly ParkWeatherLocalDateResolver localDateResolver;

    public GetParkWeatherForecastQueryHandler(
        IParkRepository parkRepository,
        IParkWeatherRepository weatherRepository,
        ParkWeatherLocalDateResolver localDateResolver)
    {
        this.parkRepository = parkRepository;
        this.weatherRepository = weatherRepository;
        this.localDateResolver = localDateResolver;
    }

    public async Task<ApplicationResult<ParkWeatherForecastResult>> HandleAsync(GetParkWeatherForecastQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), includeHidden: false, cancellationToken);
        if (park is null || !park.Status.IsOpenToVisitors())
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        if (park.Position is null || (park.Position.Latitude == 0d && park.Position.Longitude == 0d))
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(ParkWeatherApplicationErrors.ParkHasNoCoordinates(park.Id));
        }

        int dayCount = Math.Clamp(query.DayCount, 1, 7);
        ParkWeatherDailySnapshot? latestForecastSnapshot = await this.weatherRepository.GetLatestForecastSnapshotAsync(park.Id, cancellationToken);
        DateOnly parkLocalToday = this.localDateResolver.ResolveCurrentLocalDate(latestForecastSnapshot);
        IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots = await this.weatherRepository.GetForecastAsync(park.Id, parkLocalToday, dayCount, cancellationToken);
        ParkWeatherForecastResult result = new ParkWeatherForecastResult
        {
            ParkId = park.Id,
            Days = snapshots
                .OrderBy(static snapshot => snapshot.LocalDate)
                .Select(static snapshot => snapshot.ToForecastResult())
                .ToList(),
        };

        return ApplicationResult<ParkWeatherForecastResult>.Success(result);
    }
}

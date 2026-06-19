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

    public GetParkWeatherForecastQueryHandler(IParkRepository parkRepository, IParkWeatherRepository weatherRepository)
    {
        this.parkRepository = parkRepository;
        this.weatherRepository = weatherRepository;
    }

    public async Task<ApplicationResult<ParkWeatherForecastResult>> HandleAsync(GetParkWeatherForecastQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), includeHidden: false, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        if (park.Position is null || (park.Position.Latitude == 0d && park.Position.Longitude == 0d))
        {
            return ApplicationResult<ParkWeatherForecastResult>.Failure(ParkWeatherApplicationErrors.ParkHasNoCoordinates(park.Id));
        }

        int dayCount = Math.Clamp(query.DayCount, 1, 7);
        DateOnly todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots = await this.weatherRepository.GetForecastAsync(park.Id, todayUtc, dayCount, cancellationToken);
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

public sealed class GetLatestParkWeatherRunQueryHandler : IQueryHandler<GetLatestParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult?>>
{
    private readonly IParkWeatherRunRepository runRepository;

    public GetLatestParkWeatherRunQueryHandler(IParkWeatherRunRepository runRepository)
    {
        this.runRepository = runRepository;
    }

    public async Task<ApplicationResult<ParkWeatherRunResult?>> HandleAsync(GetLatestParkWeatherRunQuery query, CancellationToken cancellationToken = default)
    {
        ParkWeatherRun? run = await this.runRepository.GetLatestAsync(cancellationToken);
        return ApplicationResult<ParkWeatherRunResult?>.Success(run?.ToResult());
    }
}

public sealed class GetParkWeatherRunQueryHandler : IQueryHandler<GetParkWeatherRunQuery, ApplicationResult<ParkWeatherRunResult>>
{
    private readonly IParkWeatherRunRepository runRepository;

    public GetParkWeatherRunQueryHandler(IParkWeatherRunRepository runRepository)
    {
        this.runRepository = runRepository;
    }

    public async Task<ApplicationResult<ParkWeatherRunResult>> HandleAsync(GetParkWeatherRunQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.RunId))
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        ParkWeatherRun? run = await this.runRepository.GetByIdAsync(query.RunId.Trim(), cancellationToken);
        if (run is null)
        {
            return ApplicationResult<ParkWeatherRunResult>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        return ApplicationResult<ParkWeatherRunResult>.Success(run.ToResult());
    }
}

public sealed class GetParkWeatherRunItemsQueryHandler : IQueryHandler<GetParkWeatherRunItemsQuery, ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>>
{
    private readonly IParkWeatherRunRepository runRepository;

    public GetParkWeatherRunItemsQueryHandler(IParkWeatherRunRepository runRepository)
    {
        this.runRepository = runRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>> HandleAsync(GetParkWeatherRunItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.RunId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        ParkWeatherRun? run = await this.runRepository.GetByIdAsync(query.RunId.Trim(), cancellationToken);
        if (run is null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>.Failure(ParkWeatherApplicationErrors.RunNotFound());
        }

        ParkWeatherRunItemStatus? status = ParseStatus(query.Status);
        IReadOnlyCollection<ParkWeatherRunItem> items = await this.runRepository.GetRunItemsAsync(run.Id ?? string.Empty, status, cancellationToken);
        IReadOnlyCollection<ParkWeatherRunItemResult> results = items
            .OrderBy(static item => item.ParkName)
            .ThenBy(static item => item.ParkId)
            .Select(static item => item.ToResult())
            .ToList();

        return ApplicationResult<IReadOnlyCollection<ParkWeatherRunItemResult>>.Success(results);
    }

    private static ParkWeatherRunItemStatus? ParseStatus(string? status)
    {
        return Enum.TryParse(status, true, out ParkWeatherRunItemStatus parsed)
            ? parsed
            : null;
    }
}

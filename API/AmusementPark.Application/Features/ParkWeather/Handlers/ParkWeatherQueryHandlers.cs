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
        if (park is null)
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

public sealed class GetParkWeatherHistoricalComparisonsQueryHandler : IQueryHandler<GetParkWeatherHistoricalComparisonsQuery, ApplicationResult<ParkWeatherHistoricalComparisonsResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkWeatherRepository weatherRepository;
    private readonly ParkWeatherLocalDateResolver localDateResolver;
    private readonly ParkWeatherHistoricalComparisonDateResolver historicalComparisonDateResolver;
    private readonly IParkWeatherRefreshSettings settings;

    public GetParkWeatherHistoricalComparisonsQueryHandler(
        IParkRepository parkRepository,
        IParkWeatherRepository weatherRepository,
        ParkWeatherLocalDateResolver localDateResolver,
        ParkWeatherHistoricalComparisonDateResolver historicalComparisonDateResolver,
        IParkWeatherRefreshSettings settings)
    {
        this.parkRepository = parkRepository;
        this.weatherRepository = weatherRepository;
        this.localDateResolver = localDateResolver;
        this.historicalComparisonDateResolver = historicalComparisonDateResolver;
        this.settings = settings;
    }

    public async Task<ApplicationResult<ParkWeatherHistoricalComparisonsResult>> HandleAsync(GetParkWeatherHistoricalComparisonsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<ParkWeatherHistoricalComparisonsResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), includeHidden: false, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkWeatherHistoricalComparisonsResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        if (park.Position is null || (park.Position.Latitude == 0d && park.Position.Longitude == 0d))
        {
            return ApplicationResult<ParkWeatherHistoricalComparisonsResult>.Failure(ParkWeatherApplicationErrors.ParkHasNoCoordinates(park.Id));
        }

        int dayCount = Math.Clamp(query.DayCount, 1, 7);
        ParkWeatherDailySnapshot? latestForecastSnapshot = await this.weatherRepository.GetLatestForecastSnapshotAsync(park.Id, cancellationToken);
        DateOnly parkLocalToday = this.localDateResolver.ResolveCurrentLocalDate(latestForecastSnapshot);
        IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots = await this.weatherRepository.GetForecastAsync(park.Id, parkLocalToday, dayCount, cancellationToken);
        IReadOnlyCollection<ParkWeatherHistoricalComparisonResult> historicalComparisons = await this.GetHistoricalComparisonsAsync(
            park.Id,
            snapshots.OrderBy(static snapshot => snapshot.LocalDate).ToList(),
            Math.Min(query.YearsLimit, this.settings.HistoricalComparisonYearsLimit),
            cancellationToken);

        return ApplicationResult<ParkWeatherHistoricalComparisonsResult>.Success(new ParkWeatherHistoricalComparisonsResult
        {
            ParkId = park.Id,
            Years = historicalComparisons,
        });
    }

    private async Task<IReadOnlyCollection<ParkWeatherHistoricalComparisonResult>> GetHistoricalComparisonsAsync(
        string parkId,
        IReadOnlyCollection<ParkWeatherDailySnapshot> forecastSnapshots,
        int requestedYearsLimit,
        CancellationToken cancellationToken)
    {
        int yearsLimit = Math.Clamp(requestedYearsLimit, 0, 10);
        if (forecastSnapshots.Count == 0 || yearsLimit == 0)
        {
            return Array.Empty<ParkWeatherHistoricalComparisonResult>();
        }

        List<DateOnly> forecastDates = forecastSnapshots
            .Select(static snapshot => snapshot.LocalDate)
            .Distinct()
            .OrderBy(static date => date)
            .ToList();
        Dictionary<HistoricalComparisonKey, DateOnly> comparisonDatesByKey = new Dictionary<HistoricalComparisonKey, DateOnly>();
        List<DateOnly> comparisonDates = new List<DateOnly>();

        foreach (DateOnly forecastDate in forecastDates)
        {
            for (int yearsBack = 1; yearsBack <= yearsLimit; yearsBack += 1)
            {
                DateOnly comparisonDate = this.historicalComparisonDateResolver.ResolveComparisonDate(forecastDate, yearsBack);
                comparisonDatesByKey[new HistoricalComparisonKey(yearsBack, forecastDate)] = comparisonDate;
                comparisonDates.Add(comparisonDate);
            }
        }

        IReadOnlyCollection<ParkWeatherDailySnapshot> observations = await this.weatherRepository.GetObservationsByDatesAsync(
            parkId,
            comparisonDates,
            cancellationToken);
        Dictionary<DateOnly, ParkWeatherDailySnapshot> observationsByDate = observations
            .GroupBy(static observation => observation.LocalDate)
            .ToDictionary(static group => group.Key, static group => group.First());

        List<ParkWeatherHistoricalComparisonResult> comparisons = new List<ParkWeatherHistoricalComparisonResult>();
        for (int yearsBack = 1; yearsBack <= yearsLimit; yearsBack += 1)
        {
            List<ParkWeatherHistoricalComparisonDayResult> days = new List<ParkWeatherHistoricalComparisonDayResult>();
            foreach (DateOnly forecastDate in forecastDates)
            {
                HistoricalComparisonKey key = new HistoricalComparisonKey(yearsBack, forecastDate);
                DateOnly comparisonDate = comparisonDatesByKey[key];
                if (!observationsByDate.TryGetValue(comparisonDate, out ParkWeatherDailySnapshot? observation))
                {
                    continue;
                }

                days.Add(new ParkWeatherHistoricalComparisonDayResult
                {
                    ForecastLocalDate = forecastDate,
                    LocalDate = observation.LocalDate,
                    WeatherCode = observation.WeatherCode,
                    TemperatureMinCelsius = observation.TemperatureMinCelsius,
                    TemperatureMaxCelsius = observation.TemperatureMaxCelsius,
                    ApparentTemperatureMinCelsius = observation.ApparentTemperatureMinCelsius,
                    ApparentTemperatureMaxCelsius = observation.ApparentTemperatureMaxCelsius,
                    PrecipitationSumMillimeters = observation.PrecipitationSumMillimeters,
                    WindSpeedMaxKilometersPerHour = observation.WindSpeedMaxKilometersPerHour,
                    WindGustsMaxKilometersPerHour = observation.WindGustsMaxKilometersPerHour,
                });
            }

            if (days.Count > 0)
            {
                comparisons.Add(new ParkWeatherHistoricalComparisonResult
                {
                    YearsBack = yearsBack,
                    Days = days,
                });
            }
        }

        return comparisons;
    }

    private readonly record struct HistoricalComparisonKey(int YearsBack, DateOnly ForecastDate);
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

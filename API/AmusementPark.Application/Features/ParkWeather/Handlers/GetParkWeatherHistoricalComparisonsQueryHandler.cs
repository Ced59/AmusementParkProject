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
        if (park is null || !park.Status.IsOpenToVisitors())
        {
            return ApplicationResult<ParkWeatherHistoricalComparisonsResult>.Failure(ParkWeatherApplicationErrors.ParkNotFound());
        }

        if (park.Position is null || (park.Position.Latitude == 0d && park.Position.Longitude == 0d))
        {
            return ApplicationResult<ParkWeatherHistoricalComparisonsResult>.Failure(ParkWeatherApplicationErrors.ParkHasNoCoordinates(park.Id));
        }

        IReadOnlyCollection<DateOnly> forecastDates = NormalizeForecastDates(query.ForecastDates);
        if (forecastDates.Count == 0)
        {
            int dayCount = Math.Clamp(query.DayCount, 1, 7);
            ParkWeatherDailySnapshot? latestForecastSnapshot = await this.weatherRepository.GetLatestForecastSnapshotAsync(park.Id, cancellationToken);
            DateOnly parkLocalToday = this.localDateResolver.ResolveCurrentLocalDate(latestForecastSnapshot);
            IReadOnlyCollection<ParkWeatherDailySnapshot> snapshots = await this.weatherRepository.GetForecastAsync(park.Id, parkLocalToday, dayCount, cancellationToken);
            forecastDates = snapshots
                .Select(static snapshot => snapshot.LocalDate)
                .ToList();
        }

        IReadOnlyCollection<ParkWeatherHistoricalComparisonResult> historicalComparisons = await this.GetHistoricalComparisonsAsync(
            park.Id,
            forecastDates,
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
        IReadOnlyCollection<DateOnly> forecastDates,
        int requestedYearsLimit,
        CancellationToken cancellationToken)
    {
        int yearsLimit = Math.Clamp(requestedYearsLimit, 0, 10);
        List<DateOnly> normalizedForecastDates = NormalizeForecastDates(forecastDates);
        if (normalizedForecastDates.Count == 0 || yearsLimit == 0)
        {
            return Array.Empty<ParkWeatherHistoricalComparisonResult>();
        }

        Dictionary<HistoricalComparisonKey, DateOnly> comparisonDatesByKey = new Dictionary<HistoricalComparisonKey, DateOnly>();
        List<DateOnly> comparisonDates = new List<DateOnly>();

        foreach (DateOnly forecastDate in normalizedForecastDates)
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
            foreach (DateOnly forecastDate in normalizedForecastDates)
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

    private static List<DateOnly> NormalizeForecastDates(IReadOnlyCollection<DateOnly>? forecastDates)
    {
        if (forecastDates is null || forecastDates.Count == 0)
        {
            return new List<DateOnly>();
        }

        return forecastDates
            .Where(static date => date != default)
            .Distinct()
            .OrderBy(static date => date)
            .Take(7)
            .ToList();
    }

    private readonly record struct HistoricalComparisonKey(int YearsBack, DateOnly ForecastDate);
}

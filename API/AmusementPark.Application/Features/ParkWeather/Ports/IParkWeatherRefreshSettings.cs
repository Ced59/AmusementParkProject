namespace AmusementPark.Application.Features.ParkWeather.Ports;

public interface IParkWeatherRefreshSettings
{
    bool IsAutomaticRefreshEnabled { get; }

    int ForecastDays { get; }

    int ForecastPastRetentionDays { get; }

    bool IncludeYesterdayObservation { get; }

    int HistoricalBackfillYears { get; }

    int HistoricalComparisonYearsLimit { get; }

    int DelayBetweenParksMilliseconds { get; }

    string AutomaticRefreshTimeZoneId { get; }

    int AutomaticRefreshHour { get; }

    int AutomaticRefreshMinute { get; }
}

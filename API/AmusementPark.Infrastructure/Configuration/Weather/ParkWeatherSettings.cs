using AmusementPark.Application.Features.ParkWeather.Ports;
using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Weather;

public sealed class ParkWeatherSettings : IParkWeatherRefreshSettings
{
    public const string SectionName = "ParkWeather";

    public bool IsAutomaticRefreshEnabled { get; set; } = true;

    public string ActiveProviderKey { get; set; } = "open-meteo";

    public int ForecastDays { get; set; } = 7;

    public int ForecastPastRetentionDays { get; set; } = 3;

    public bool IncludeYesterdayObservation { get; set; } = true;

    public int HistoricalBackfillYears { get; set; } = 3;

    public int HistoricalComparisonYearsLimit { get; set; } = 10;

    public int DelayBetweenParksMilliseconds { get; set; } = 250;

    public int MinimumDelayBetweenProviderRequestsMilliseconds { get; set; } = 750;

    public string AutomaticRefreshTimeZoneId { get; set; } = "Europe/Paris";

    public int AutomaticRefreshHour { get; set; } = 2;

    public int AutomaticRefreshMinute { get; set; } = 15;

    public int RequestTimeoutSeconds { get; set; } = 20;

    public string OpenMeteoForecastBaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

    public string OpenMeteoArchiveBaseUrl { get; set; } = "https://archive-api.open-meteo.com/v1/archive";

    public static ParkWeatherSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        ParkWeatherSettings settings = configuration.GetSection(SectionName).Get<ParkWeatherSettings>() ?? new ParkWeatherSettings();

        if (string.IsNullOrWhiteSpace(settings.ActiveProviderKey))
        {
            settings.ActiveProviderKey = "open-meteo";
        }

        settings.ForecastDays = Math.Clamp(settings.ForecastDays, 1, 7);
        settings.ForecastPastRetentionDays = Math.Clamp(settings.ForecastPastRetentionDays, 0, 30);
        settings.HistoricalBackfillYears = Math.Clamp(settings.HistoricalBackfillYears, 0, 3);
        settings.HistoricalComparisonYearsLimit = Math.Clamp(settings.HistoricalComparisonYearsLimit, 0, 10);
        settings.DelayBetweenParksMilliseconds = Math.Clamp(settings.DelayBetweenParksMilliseconds, 0, 60_000);
        settings.MinimumDelayBetweenProviderRequestsMilliseconds = Math.Clamp(settings.MinimumDelayBetweenProviderRequestsMilliseconds, 0, 60_000);
        settings.AutomaticRefreshHour = Math.Clamp(settings.AutomaticRefreshHour, 0, 23);
        settings.AutomaticRefreshMinute = Math.Clamp(settings.AutomaticRefreshMinute, 0, 59);
        settings.RequestTimeoutSeconds = Math.Clamp(settings.RequestTimeoutSeconds, 2, 120);

        if (string.IsNullOrWhiteSpace(settings.AutomaticRefreshTimeZoneId))
        {
            settings.AutomaticRefreshTimeZoneId = "Europe/Paris";
        }

        if (string.IsNullOrWhiteSpace(settings.OpenMeteoForecastBaseUrl))
        {
            settings.OpenMeteoForecastBaseUrl = "https://api.open-meteo.com/v1/forecast";
        }

        if (string.IsNullOrWhiteSpace(settings.OpenMeteoArchiveBaseUrl))
        {
            settings.OpenMeteoArchiveBaseUrl = "https://archive-api.open-meteo.com/v1/archive";
        }

        return settings;
    }
}

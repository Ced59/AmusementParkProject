using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Services;

internal sealed class ParkWeatherRefreshOrchestratorTestsTestRefreshSettings : IParkWeatherRefreshSettings
{
    public bool IsAutomaticRefreshEnabled => true;

    public int ForecastDays => 7;

    public int ForecastPastRetentionDays => 3;

    public bool IncludeYesterdayObservation => true;

    public int HistoricalBackfillYears { get; init; }

    public int HistoricalComparisonYearsLimit { get; init; } = 10;

    public int DelayBetweenParksMilliseconds => 0;

    public string AutomaticRefreshTimeZoneId => "UTC";

    public int AutomaticRefreshHour => 2;

    public int AutomaticRefreshMinute => 15;
}

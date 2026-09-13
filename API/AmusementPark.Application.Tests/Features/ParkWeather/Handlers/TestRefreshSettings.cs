using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkWeather.Handlers;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.Application.Features.ParkWeather.Queries;
using AmusementPark.Application.Features.ParkWeather.Results;
using AmusementPark.Application.Features.ParkWeather.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Weather;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkWeather.Handlers;

internal sealed class TestRefreshSettings : IParkWeatherRefreshSettings
{
    public bool IsAutomaticRefreshEnabled => true;

    public int ForecastDays => 7;

    public int ForecastPastRetentionDays => 3;

    public bool IncludeYesterdayObservation => true;

    public int HistoricalBackfillYears => 3;

    public int HistoricalComparisonYearsLimit => 2;

    public int DelayBetweenParksMilliseconds => 0;

    public string AutomaticRefreshTimeZoneId => "UTC";

    public int AutomaticRefreshHour => 2;

    public int AutomaticRefreshMinute => 15;
}

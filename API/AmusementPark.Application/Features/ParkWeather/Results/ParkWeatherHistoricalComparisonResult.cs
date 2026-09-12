using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherHistoricalComparisonResult
{
    public int YearsBack { get; init; }

    public IReadOnlyCollection<ParkWeatherHistoricalComparisonDayResult> Days { get; init; } = Array.Empty<ParkWeatherHistoricalComparisonDayResult>();
}

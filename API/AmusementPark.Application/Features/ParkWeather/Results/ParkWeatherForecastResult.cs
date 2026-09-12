using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherForecastResult
{
    public string ParkId { get; init; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherDailyForecastResult> Days { get; init; } = Array.Empty<ParkWeatherDailyForecastResult>();

    public ParkWeatherAttributionResult Attribution { get; init; } = new ParkWeatherAttributionResult();
}

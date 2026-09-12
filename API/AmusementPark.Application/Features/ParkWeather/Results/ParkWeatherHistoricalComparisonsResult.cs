using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherHistoricalComparisonsResult
{
    public string ParkId { get; init; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherHistoricalComparisonResult> Years { get; init; } = Array.Empty<ParkWeatherHistoricalComparisonResult>();

    public ParkWeatherAttributionResult Attribution { get; init; } = new ParkWeatherAttributionResult();
}

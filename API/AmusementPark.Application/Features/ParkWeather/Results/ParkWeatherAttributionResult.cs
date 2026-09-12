using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Results;

public sealed class ParkWeatherAttributionResult
{
    public string ProviderName { get; init; } = "Open-Meteo";

    public string ProviderUrl { get; init; } = "https://open-meteo.com/";

    public string LicenseName { get; init; } = "CC BY 4.0";

    public string LicenseUrl { get; init; } = "https://creativecommons.org/licenses/by/4.0/";
}

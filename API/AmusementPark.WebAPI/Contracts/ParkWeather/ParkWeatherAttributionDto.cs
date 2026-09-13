namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherAttributionDto
{
    public string ProviderName { get; set; } = string.Empty;

    public string ProviderUrl { get; set; } = string.Empty;

    public string LicenseName { get; set; } = string.Empty;

    public string LicenseUrl { get; set; } = string.Empty;
}

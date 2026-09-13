namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherForecastDto
{
    public string ParkId { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherDailyForecastDto> Days { get; set; } = Array.Empty<ParkWeatherDailyForecastDto>();

    public ParkWeatherAttributionDto Attribution { get; set; } = new ParkWeatherAttributionDto();
}

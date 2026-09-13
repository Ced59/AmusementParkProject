namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherHistoricalComparisonsDto
{
    public string ParkId { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkWeatherHistoricalComparisonDto> Years { get; set; } = Array.Empty<ParkWeatherHistoricalComparisonDto>();

    public ParkWeatherAttributionDto Attribution { get; set; } = new ParkWeatherAttributionDto();
}

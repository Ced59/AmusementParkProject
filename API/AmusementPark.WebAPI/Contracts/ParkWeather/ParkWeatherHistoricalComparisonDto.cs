namespace AmusementPark.WebAPI.Contracts.ParkWeather;

public sealed class ParkWeatherHistoricalComparisonDto
{
    public int YearsBack { get; set; }

    public IReadOnlyCollection<ParkWeatherHistoricalComparisonDayDto> Days { get; set; } = Array.Empty<ParkWeatherHistoricalComparisonDayDto>();
}

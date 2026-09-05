namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportGlobalStatisticsDto
{
    public int? SelectedYear { get; init; }
    public string? SelectedParkId { get; init; }
    public IReadOnlyCollection<int> AvailableYears { get; init; } = Array.Empty<int>();
    public IReadOnlyCollection<PassportGlobalFilterParkDto> AvailableParks { get; init; } =
        Array.Empty<PassportGlobalFilterParkDto>();
    public long ParkCount { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
    public IReadOnlyCollection<PassportGlobalYearActivityDto> ActivityByYear { get; init; } =
        Array.Empty<PassportGlobalYearActivityDto>();
    public IReadOnlyCollection<PassportGlobalParkActivityDto> TopParks { get; init; } =
        Array.Empty<PassportGlobalParkActivityDto>();
    public IReadOnlyCollection<PassportGlobalItemActivityDto> TopItems { get; init; } =
        Array.Empty<PassportGlobalItemActivityDto>();
    public IReadOnlyCollection<PassportGlobalRatingEvolutionDto> RatingEvolution { get; init; } =
        Array.Empty<PassportGlobalRatingEvolutionDto>();
}

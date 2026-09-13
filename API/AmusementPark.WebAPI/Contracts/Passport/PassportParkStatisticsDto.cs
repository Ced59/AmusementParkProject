namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportParkStatisticsDto
{
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
    public double? CurrentGlobalRating { get; init; }
    public double? CurrentGlobalMinusHistoricalAverage { get; init; }
    public IReadOnlyCollection<PassportParkAssessmentPointDto> AssessmentTimeline { get; init; } =
        Array.Empty<PassportParkAssessmentPointDto>();
    public IReadOnlyCollection<PassportYearBreakdownDto> ByYear { get; init; } =
        Array.Empty<PassportYearBreakdownDto>();
    public IReadOnlyCollection<PassportCurrentItemRatingDto> CurrentTopItems { get; init; } =
        Array.Empty<PassportCurrentItemRatingDto>();
    public IReadOnlyCollection<PassportHistoricalItemRatingDto> HistoricalTopItems { get; init; } =
        Array.Empty<PassportHistoricalItemRatingDto>();
}

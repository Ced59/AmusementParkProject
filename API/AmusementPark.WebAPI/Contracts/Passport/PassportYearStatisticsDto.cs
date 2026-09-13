namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportYearStatisticsDto
{
    public int Year { get; init; }
    public long ParkCount { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
    public IReadOnlyCollection<PassportParkBreakdownDto> ByPark { get; init; } =
        Array.Empty<PassportParkBreakdownDto>();
}

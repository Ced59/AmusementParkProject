namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportYearBreakdownDto
{
    public int Year { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
}

namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportParkBreakdownDto
{
    public string ParkId { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public PassportStatisticsSummaryDto Summary { get; init; } =
        new PassportStatisticsSummaryDto();
}

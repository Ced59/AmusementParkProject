namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportParkBreakdownResult(
    string ParkId,
    PassportStatisticsSummaryResult Summary,
    string? ParkName = null);

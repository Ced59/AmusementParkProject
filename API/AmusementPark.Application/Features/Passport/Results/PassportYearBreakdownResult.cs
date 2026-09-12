namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportYearBreakdownResult(
    int Year,
    PassportStatisticsSummaryResult Summary);

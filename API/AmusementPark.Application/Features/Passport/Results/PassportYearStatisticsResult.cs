namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportYearStatisticsResult(
    int Year,
    long ParkCount,
    PassportStatisticsSummaryResult Summary,
    IReadOnlyCollection<PassportParkBreakdownResult> ByPark);

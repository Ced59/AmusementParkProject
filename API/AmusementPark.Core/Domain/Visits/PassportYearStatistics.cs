namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportYearStatistics(
    int Year,
    long ParkCount,
    PassportStatisticsSummary Summary,
    IReadOnlyCollection<PassportParkBreakdown> ByPark);

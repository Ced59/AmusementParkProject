namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportProfileStatistics(
    long ParkCount,
    PassportStatisticsSummary Summary,
    IReadOnlyCollection<PassportProfileYearStatistics> Years,
    IReadOnlyCollection<PassportProfileParkStatistics> Parks);

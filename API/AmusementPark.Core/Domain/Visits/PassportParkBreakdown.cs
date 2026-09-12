namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportParkBreakdown(
    string ParkId,
    PassportStatisticsSummary Summary);

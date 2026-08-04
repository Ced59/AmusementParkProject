namespace AmusementPark.Core.Domain.Parks;

public static class ParkStatusExtensions
{
    public static bool IsSupported(this ParkStatus status)
    {
        return status is ParkStatus.Operating
            or ParkStatus.ClosedDefinitively
            or ParkStatus.Planned
            or ParkStatus.UnderConstruction
            or ParkStatus.TemporarilyClosed
            or ParkStatus.Cancelled;
    }

    public static bool IsOpenToVisitors(this ParkStatus status)
    {
        return status == ParkStatus.Operating;
    }

    public static bool CanHaveCurrentOpeningHours(this ParkStatus status)
    {
        return status.IsOpenToVisitors();
    }

    public static bool CanReceiveVisitorRatings(this ParkStatus status)
    {
        return status is ParkStatus.Operating
            or ParkStatus.TemporarilyClosed
            or ParkStatus.ClosedDefinitively;
    }

    public static bool CanAppearInCurrentRatingRankings(this ParkStatus status)
    {
        return status == ParkStatus.Operating;
    }

    public static bool IsFutureProject(this ParkStatus status)
    {
        return status is ParkStatus.Planned or ParkStatus.UnderConstruction;
    }

    public static bool IsHistoricalOrCancelled(this ParkStatus status)
    {
        return status is ParkStatus.ClosedDefinitively or ParkStatus.Cancelled;
    }

    public static bool CanAppearInPublicDiscovery(this ParkStatus status)
    {
        return status.IsSupported();
    }
}

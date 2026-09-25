namespace AmusementPark.Core.Domain.Trips;

public static class TripPassportTransitionPolicy
{
    public static bool CanConfirmDay(
        DateOnly localDate,
        DateOnly destinationToday,
        bool isParkAvailable)
    {
        return isParkAvailable && localDate < destinationToday;
    }
}

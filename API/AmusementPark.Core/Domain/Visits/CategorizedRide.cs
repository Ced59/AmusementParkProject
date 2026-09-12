namespace AmusementPark.Core.Domain.Visits;

internal sealed record CategorizedRide(
    PassportRideStatisticsObservation Ride,
    string? Category,
    bool UsesHistoricalCategory,
    bool UsesCurrentCategory);

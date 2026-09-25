namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPassportTransitionResult(
    string Title,
    DateOnly DestinationToday,
    IReadOnlyCollection<TripPassportTransitionDayResult> Days);

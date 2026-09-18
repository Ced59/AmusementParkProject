namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripActivityPageResult(
    string TripTitle,
    IReadOnlyCollection<TripActivityEntryResult> Entries,
    long? NextBeforeSequence);

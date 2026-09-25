namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripActivityPageDto(
    string TripTitle,
    IReadOnlyCollection<TripActivityEntryDto> Entries,
    long? NextBeforeSequence);

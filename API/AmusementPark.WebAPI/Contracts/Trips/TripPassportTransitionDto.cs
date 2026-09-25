namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPassportTransitionDto
{
    public string Title { get; set; } = string.Empty;

    public DateOnly DestinationToday { get; set; }

    public IReadOnlyCollection<TripPassportTransitionDayDto> Days { get; set; } =
        Array.Empty<TripPassportTransitionDayDto>();
}

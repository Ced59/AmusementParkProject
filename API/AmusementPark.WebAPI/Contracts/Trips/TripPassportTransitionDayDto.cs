namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPassportTransitionDayDto
{
    public DateOnly LocalDate { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public bool IsParkAvailable { get; set; }

    public bool CanConfirm { get; set; }

    public bool CanResume { get; set; }

    public bool IsSelectionLocked { get; set; }

    public string? ExistingVisitId { get; set; }

    public string? ExistingVisitStatus { get; set; }

    public IReadOnlyCollection<TripPassportTransitionItemDto> Attractions { get; set; } =
        Array.Empty<TripPassportTransitionItemDto>();
}

namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripParticipantDto
{
    public string MemberId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public bool IsCurrentUser { get; set; }

    public DateTime JoinedAtUtc { get; set; }
}

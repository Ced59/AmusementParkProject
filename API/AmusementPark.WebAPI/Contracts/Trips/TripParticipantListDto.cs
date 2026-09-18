namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripParticipantListDto
{
    public IReadOnlyCollection<TripParticipantDto> Participants { get; set; } = Array.Empty<TripParticipantDto>();

    public bool CanManageRoles { get; set; }

    public bool CanTransferOwnership { get; set; }

    public bool CanLeave { get; set; }

    public long TripVersion { get; set; }
}

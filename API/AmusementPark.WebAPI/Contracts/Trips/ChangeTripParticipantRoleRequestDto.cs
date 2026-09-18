namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class ChangeTripParticipantRoleRequestDto
{
    public string Role { get; set; } = string.Empty;

    public long ExpectedVersion { get; set; }
}

namespace AmusementPark.Application.Features.Sharing.Ports;

public enum SharePublicationWriteOutcome
{
    Success = 0,
    Conflict = 1,
    TokenCollision = 2,
}

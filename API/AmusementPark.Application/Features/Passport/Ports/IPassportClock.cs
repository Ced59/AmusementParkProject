namespace AmusementPark.Application.Features.Passport.Ports;

public interface IPassportClock
{
    DateTime UtcNow { get; }
}

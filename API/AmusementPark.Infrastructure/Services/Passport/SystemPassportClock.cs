using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class SystemPassportClock : IPassportClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

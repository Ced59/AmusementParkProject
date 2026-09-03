namespace AmusementPark.Application.Features.Passport.Ports;

public interface IPassportLocalDateResolver
{
    DateOnly Resolve(DateTime utcNow, string? timeZoneId);
}

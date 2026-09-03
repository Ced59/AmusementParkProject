namespace AmusementPark.Application.Features.Passport.Ports;

public interface IPassportTimeZoneValidator
{
    bool IsValid(string timeZoneId);
}

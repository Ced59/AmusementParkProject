namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripTimeZoneValidator
{
    bool IsValidIanaTimeZone(string timeZoneId);
}

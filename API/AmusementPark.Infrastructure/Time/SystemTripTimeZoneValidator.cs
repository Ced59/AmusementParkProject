using AmusementPark.Application.Features.Trips.Ports;

namespace AmusementPark.Infrastructure.Time;

public sealed class SystemTripTimeZoneValidator : ITripTimeZoneValidator
{
    public bool IsValidIanaTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        string normalized = timeZoneId.Trim();
        if (!string.Equals(normalized, "UTC", StringComparison.Ordinal)
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(normalized, out string? _))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(normalized, out TimeZoneInfo? _);
    }
}

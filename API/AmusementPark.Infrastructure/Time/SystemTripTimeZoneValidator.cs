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
        if (string.Equals(normalized, "Etc/UTC", StringComparison.Ordinal))
        {
            return true;
        }

        return normalized.Contains('/', StringComparison.Ordinal)
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(normalized, out string? _);
    }
}

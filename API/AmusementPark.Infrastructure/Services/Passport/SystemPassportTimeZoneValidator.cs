using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class SystemPassportTimeZoneValidator : IPassportTimeZoneValidator
{
    public bool IsValid(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        string normalizedTimeZoneId = timeZoneId.Trim();
        if (!TimeZoneInfo.TryConvertIanaIdToWindowsId(
                normalizedTimeZoneId,
                out string? windowsTimeZoneId)
            || string.IsNullOrWhiteSpace(windowsTimeZoneId))
        {
            return false;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById(normalizedTimeZoneId, out _))
        {
            return true;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(windowsTimeZoneId, out _);
    }
}

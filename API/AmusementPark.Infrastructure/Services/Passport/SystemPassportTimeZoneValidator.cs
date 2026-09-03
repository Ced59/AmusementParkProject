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
        if (TimeZoneInfo.TryFindSystemTimeZoneById(normalizedTimeZoneId, out _))
        {
            return true;
        }

        if (TimeZoneInfo.TryConvertIanaIdToWindowsId(
                normalizedTimeZoneId,
                out string? windowsTimeZoneId)
            && !string.IsNullOrWhiteSpace(windowsTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsTimeZoneId, out _))
        {
            return true;
        }

        return TimeZoneInfo.TryConvertWindowsIdToIanaId(
                normalizedTimeZoneId,
                out string? ianaTimeZoneId)
            && !string.IsNullOrWhiteSpace(ianaTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaTimeZoneId, out _);
    }
}

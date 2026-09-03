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
        if (!string.Equals(normalizedTimeZoneId, "UTC", StringComparison.Ordinal)
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(
                normalizedTimeZoneId,
                out _))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(normalizedTimeZoneId, out _);
    }
}

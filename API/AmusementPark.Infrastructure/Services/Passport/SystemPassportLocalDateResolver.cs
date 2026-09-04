using AmusementPark.Application.Features.Passport.Ports;

namespace AmusementPark.Infrastructure.Services.Passport;

public sealed class SystemPassportLocalDateResolver : IPassportLocalDateResolver
{
    public DateOnly Resolve(DateTime utcNow, string? timeZoneId)
    {
        if (utcNow.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The current time must be UTC.", nameof(utcNow));
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return DateOnly.FromDateTime(utcNow);
        }

        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZone);
        return DateOnly.FromDateTime(localTime);
    }
}

using AmusementPark.Core.Domain.Weather;

namespace AmusementPark.Application.Features.ParkWeather.Services;

public sealed class ParkWeatherLocalDateResolver
{
    private readonly TimeProvider timeProvider;

    public ParkWeatherLocalDateResolver()
        : this(TimeProvider.System)
    {
    }

    public ParkWeatherLocalDateResolver(TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
    }

    public DateOnly ResolveCurrentLocalDate(ParkWeatherDailySnapshot? referenceSnapshot)
    {
        return ResolveLocalDate(
            this.timeProvider.GetUtcNow().UtcDateTime,
            referenceSnapshot?.TimeZone,
            referenceSnapshot?.UtcOffsetSeconds);
    }

    public static DateOnly ResolveLocalDate(DateTime utcDateTime, string? timeZoneId, int? utcOffsetSeconds)
    {
        DateTime normalizedUtc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
                return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(normalizedUtc, timeZone));
            }
            catch (TimeZoneNotFoundException)
            {
                // Fall back to the provider offset when the host OS cannot resolve the provider timezone id.
            }
            catch (InvalidTimeZoneException)
            {
                // Fall back to the provider offset when the host OS cannot resolve the provider timezone id.
            }
        }

        if (utcOffsetSeconds.HasValue)
        {
            return DateOnly.FromDateTime(normalizedUtc.AddSeconds(utcOffsetSeconds.Value));
        }

        return DateOnly.FromDateTime(normalizedUtc);
    }
}

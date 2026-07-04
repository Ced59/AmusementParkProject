namespace AmusementPark.Core.Domain.Parks;

internal static class ParkOpeningHoursTimeZoneResolver
{
    public static DateOnly ResolveLocalDate(string timeZoneId, DateTime utcNow)
    {
        TimeZoneInfo timeZone = ResolveTimeZone(timeZoneId);
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        return DateOnly.FromDateTime(localNow);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out TimeZoneInfo? directTimeZone))
        {
            return directTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out string? windowsTimeZoneId)
            && !string.IsNullOrWhiteSpace(windowsTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsTimeZoneId, out TimeZoneInfo? windowsTimeZone))
        {
            return windowsTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId.Trim(), out string? ianaTimeZoneId)
            && !string.IsNullOrWhiteSpace(ianaTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaTimeZoneId, out TimeZoneInfo? ianaTimeZone))
        {
            return ianaTimeZone;
        }

        return TimeZoneInfo.Utc;
    }
}

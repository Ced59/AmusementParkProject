namespace AmusementPark.Core.Domain.Watchlists;

public static class NotificationDigestPeriodResolver
{
    public static DateTime ResolveStart(NotificationFrequency frequency, DateTime occurredAtUtc)
    {
        EnsureUtc(occurredAtUtc, nameof(occurredAtUtc));
        DateTime dayStart = occurredAtUtc.Date;
        return frequency switch
        {
            NotificationFrequency.DailyDigest => dayStart,
            NotificationFrequency.WeeklyDigest => dayStart.AddDays(
                -(((int)dayStart.DayOfWeek + 6) % 7)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(frequency),
                "Only digest frequencies define a digest period."),
        };
    }

    public static DateTime ResolveEnd(NotificationFrequency frequency, DateTime periodStartUtc)
    {
        ValidateGroup(NotificationChannel.Email, frequency, periodStartUtc);
        return frequency == NotificationFrequency.DailyDigest
            ? periodStartUtc.AddDays(1)
            : periodStartUtc.AddDays(7);
    }

    public static void ValidateGroup(
        NotificationChannel channel,
        NotificationFrequency frequency,
        DateTime periodStartUtc)
    {
        if (channel != NotificationChannel.Email)
        {
            throw new ArgumentOutOfRangeException(nameof(channel), "The digest channel is not supported.");
        }

        EnsureUtc(periodStartUtc, nameof(periodStartUtc));
        DateTime expectedStart = ResolveStartWithoutValidation(frequency, periodStartUtc);
        if (periodStartUtc != expectedStart)
        {
            throw new ArgumentException("The digest period start is not aligned to its UTC period.", nameof(periodStartUtc));
        }
    }

    private static DateTime ResolveStartWithoutValidation(
        NotificationFrequency frequency,
        DateTime timestampUtc)
    {
        DateTime dayStart = timestampUtc.Date;
        return frequency switch
        {
            NotificationFrequency.DailyDigest => dayStart,
            NotificationFrequency.WeeklyDigest => dayStart.AddDays(
                -(((int)dayStart.DayOfWeek + 6) % 7)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(frequency),
                "Only digest frequencies define a digest period."),
        };
    }

    private static void EnsureUtc(DateTime timestamp, string parameterName)
    {
        if (timestamp.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Digest timestamps must use UTC.", parameterName);
        }
    }
}

namespace AmusementPark.Core.Domain.LiveData;

public sealed class LivePollingActiveWindow
{
    public LivePollingActiveWindow(TimeZoneInfo timeZone, int startsAtHour, int endsAtHour)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        if (startsAtHour is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(startsAtHour));
        }

        if (endsAtHour is < 1 or > 24 || startsAtHour >= endsAtHour)
        {
            throw new ArgumentOutOfRangeException(nameof(endsAtHour));
        }

        this.TimeZone = timeZone;
        this.StartsAtHour = startsAtHour;
        this.EndsAtHour = endsAtHour;
    }

    public TimeZoneInfo TimeZone { get; }

    public int StartsAtHour { get; }

    public int EndsAtHour { get; }

    public bool Contains(DateTime utcTimestamp)
    {
        ValidateUtc(utcTimestamp);
        DateTime localTimestamp = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, this.TimeZone);
        return localTimestamp.Hour >= this.StartsAtHour && localTimestamp.Hour < this.EndsAtHour;
    }

    public DateTime GetNextOpeningUtc(DateTime utcTimestamp)
    {
        ValidateUtc(utcTimestamp);
        DateTime localTimestamp = TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, this.TimeZone);
        DateTime localOpening = localTimestamp.Hour < this.StartsAtHour
            ? localTimestamp.Date.AddHours(this.StartsAtHour)
            : localTimestamp.Date.AddDays(1).AddHours(this.StartsAtHour);

        while (this.TimeZone.IsInvalidTime(localOpening))
        {
            localOpening = localOpening.AddMinutes(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(localOpening, DateTimeKind.Unspecified),
            this.TimeZone);
    }

    private static void ValidateUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new LiveDataValidationException(
                LiveDataErrorCodes.InvalidTimestamp,
                "An active-window timestamp must be UTC.",
                nameof(value));
        }
    }
}

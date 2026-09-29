namespace AmusementPark.Core.Domain.LiveData;

internal sealed record LiveWaitForecastHourlyPoint(
    DateTime TimestampUtc,
    DateTime AvailableAtUtc,
    DateOnly LocalDate,
    DayOfWeek DayOfWeek,
    int LocalHour,
    double WaitMinutes);

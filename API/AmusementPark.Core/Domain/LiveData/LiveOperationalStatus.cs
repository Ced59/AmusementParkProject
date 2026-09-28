namespace AmusementPark.Core.Domain.LiveData;

public enum LiveOperationalStatus
{
    Open = 1,
    Closed = 2,
    TemporarilyClosed = 3,
    Delayed = 4,
    Down = 5,
    WeatherClosed = 6,
    Maintenance = 7,
    OperatingWithLimitations = 8,
    Unknown = 9,
    NotOperatingToday = 10,
    Removed = 11,
}

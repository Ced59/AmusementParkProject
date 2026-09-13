namespace AmusementPark.Core.Domain.Weather;

public enum ParkWeatherRunStatus
{
    Queued,
    Running,
    Completed,
    CompletedWithFailures,
    Failed,
    Skipped,
}

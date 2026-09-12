namespace AmusementPark.Core.Domain.Weather;

public enum ParkWeatherDataKind
{
    Forecast,
    Observation,
}

public enum ParkWeatherRefreshScope
{
    FullVisibleParks,
    FailedFromRun,
    SinglePark,
}

public enum ParkWeatherRunTrigger
{
    Automatic,
    Manual,
    RetryFailed,
    RetryPark,
}

public enum ParkWeatherRunStatus
{
    Queued,
    Running,
    Completed,
    CompletedWithFailures,
    Failed,
    Skipped,
}

public enum ParkWeatherRunItemStatus
{
    Pending,
    Running,
    Succeeded,
    Failed,
    Skipped,
}

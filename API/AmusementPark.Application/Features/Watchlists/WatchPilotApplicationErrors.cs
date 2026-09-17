using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists;

public static class WatchPilotApplicationErrors
{
    public static ApplicationError InvalidInteraction()
    {
        return ApplicationError.Validation(
            "watch-pilot.interaction.invalid",
            "The watch pilot interaction is invalid.");
    }

    public static ApplicationError InvalidMetricsRange()
    {
        return ApplicationError.Validation(
            "watch-pilot.metrics-range.invalid",
            "The watch pilot metrics range is invalid.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "watch-pilot.interaction.changed-concurrently",
            "The notification changed before the interaction could be recorded.");
    }
}

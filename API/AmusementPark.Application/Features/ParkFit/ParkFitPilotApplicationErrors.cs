using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkFit;

public static class ParkFitPilotApplicationErrors
{
    public static ApplicationError InvalidObservation()
    {
        return ApplicationError.Validation(
            "park-fit.pilot.observation.invalid",
            "The Park Fit pilot observation is invalid.");
    }

    public static ApplicationError InvalidMetricsRange()
    {
        return ApplicationError.Validation(
            "park-fit.pilot.metrics-range.invalid",
            "The Park Fit pilot metrics range is invalid.");
    }
}

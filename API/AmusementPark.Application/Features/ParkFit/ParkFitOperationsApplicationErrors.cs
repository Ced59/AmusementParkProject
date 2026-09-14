using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkFit;

public static class ParkFitOperationsApplicationErrors
{
    public static ApplicationError InvalidReport()
    {
        return ApplicationError.Validation(
            "park-fit.report.invalid",
            "The Park Fit source report is invalid.");
    }

    public static ApplicationError ParkNotFound()
    {
        return ApplicationError.NotFound(
            "park-fit.park.not-found",
            "The Park Fit park was not found.");
    }

    public static ApplicationError ReportNotFound()
    {
        return ApplicationError.NotFound(
            "park-fit.report.not-found",
            "The Park Fit source report was not found.");
    }

    public static ApplicationError InvalidTransition()
    {
        return ApplicationError.RuleViolation(
            "park-fit.operations.invalid-transition",
            "The requested Park Fit transition is not allowed.");
    }

    public static ApplicationError ActivationQualityRequired()
    {
        return ApplicationError.RuleViolation(
            "park-fit.operations.activation-quality-required",
            "The park must pass the Park Fit quality assessment before activation.");
    }

    public static ApplicationError Conflict()
    {
        return ApplicationError.Conflict(
            "park-fit.operations.conflict",
            "The Park Fit operational data changed in the meantime.");
    }

    public static ApplicationError InvalidSearch()
    {
        return ApplicationError.Validation(
            "park-fit.reports.search.invalid",
            "The Park Fit report search is invalid.");
    }
}

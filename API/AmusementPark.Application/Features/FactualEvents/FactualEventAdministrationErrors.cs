using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.FactualEvents;

public static class FactualEventAdministrationErrors
{
    public static ApplicationError InvalidSearch()
    {
        return ApplicationError.Validation(
            "factual-event.admin.search.invalid",
            "The factual event search is invalid.");
    }

    public static ApplicationError InvalidMutation()
    {
        return ApplicationError.Validation(
            "factual-event.admin.mutation.invalid",
            "The factual event mutation is invalid.");
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound(
            "factual-event.not-found",
            "The factual event does not exist.");
    }

    public static ApplicationError Conflict()
    {
        return ApplicationError.Conflict(
            "factual-event.version.conflict",
            "The factual event changed while it was being reviewed.");
    }

    public static ApplicationError InvalidTransition()
    {
        return ApplicationError.RuleViolation(
            "factual-event.transition.invalid",
            "The factual event cannot move to the requested status.");
    }
}

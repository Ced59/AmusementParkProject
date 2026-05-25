using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkGraphUpserts;

internal static class ParkGraphUpsertApplicationErrors
{
    public static ApplicationError InvalidDocument(string message)
    {
        return ApplicationError.Validation("park-graph-upsert.invalid-document", message);
    }

    public static ApplicationError CannotApply(string message)
    {
        return ApplicationError.RuleViolation("park-graph-upsert.cannot-apply", message);
    }
}

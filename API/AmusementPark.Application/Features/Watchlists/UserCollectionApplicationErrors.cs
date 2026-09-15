using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists;

public static class UserCollectionApplicationErrors
{
    public static ApplicationError Invalid(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError TargetNotFound()
    {
        return ApplicationError.NotFound(
            "collection.target-not-found",
            "The public collection target was not found.");
    }

    public static ApplicationError LimitReached()
    {
        return ApplicationError.RuleViolation(
            "collection.limit-reached",
            $"At most {UserCollectionEntry.MaximumEntriesPerUser} collection entries are allowed per user.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "collection.changed-concurrently",
            "The collection changed before this action completed.");
    }
}

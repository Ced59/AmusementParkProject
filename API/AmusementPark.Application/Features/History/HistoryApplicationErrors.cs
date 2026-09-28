using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.History;

internal static class HistoryApplicationErrors
{
    public static ApplicationError HistoryNotFound()
    {
        return ApplicationError.NotFound("history.not-found", "No public history is available for this resource.");
    }

    public static ApplicationError ArticleNotFound()
    {
        return ApplicationError.NotFound("history.article.not-found", "No public history article is available for this event.");
    }

    public static ApplicationError InvalidOwner()
    {
        return ApplicationError.Validation("history.owner.invalid", "The history event owner is invalid.");
    }

    public static ApplicationError InvalidDate()
    {
        return ApplicationError.Validation("history.date.invalid", "The history event date is invalid.");
    }

    public static ApplicationError InvalidEventType()
    {
        return ApplicationError.Validation("history.event-type.invalid", "The history event type is invalid for the selected owner.");
    }

    public static ApplicationError InvalidSnapshotDate()
    {
        return ApplicationError.Validation(
            "history.snapshot.date.invalid",
            "The requested historical snapshot date is invalid.");
    }

    public static ApplicationError InvalidComparisonRange()
    {
        return ApplicationError.Validation(
            "history.comparison.range.invalid",
            "The historical comparison start year must precede its valid end year.");
    }

    public static ApplicationError InvalidEditorialResource(string message)
    {
        return ApplicationError.Validation(
            "history.editorial.invalid",
            message);
    }

    public static ApplicationError EditorialResourceNotFound(string resourceType, Guid resourceId)
    {
        return ApplicationError.NotFound(
            "history.editorial.not-found",
            $"The historical {resourceType} '{resourceId}' was not found.");
    }

    public static ApplicationError EditorialRevisionConflict(int currentRevision)
    {
        return ApplicationError.Conflict(
            "history.editorial.revision-conflict",
            "The historical resource changed during this edit. Reload its latest revision.",
            currentRevision);
    }
}

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
}

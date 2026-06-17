using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.SocialShare;

public static class SocialShareApplicationErrors
{
    public static ApplicationError InvalidEvent(IReadOnlyDictionary<string, IReadOnlyCollection<string>> details)
    {
        return ApplicationError.Validation("social-share.event.invalid", "L'evenement de partage est invalide.", details);
    }

    public static ApplicationError InvalidDateRange()
    {
        return ApplicationError.Validation("social-share.stats.date-range.invalid", "La periode de statistiques est invalide.");
    }
}

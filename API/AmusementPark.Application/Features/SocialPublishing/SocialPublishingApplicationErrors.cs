using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.SocialPublishing;

public static class SocialPublishingApplicationErrors
{
    public static ApplicationError InvalidNetwork()
    {
        return ApplicationError.Validation(
            "social-publishing.network.invalid",
            "Le réseau social demandé n'est pas pris en charge.");
    }

    public static ApplicationError InvalidMessage()
    {
        return ApplicationError.Validation(
            "social-publishing.message.invalid",
            "Le texte de publication doit contenir entre 1 et 5 000 caractères.");
    }

    public static ApplicationError InvalidUrl()
    {
        return ApplicationError.Validation(
            "social-publishing.url.invalid",
            "Le lien doit être une URL publique du site Amusement Parks.");
    }

    public static ApplicationError PublicationNotFound(string id)
    {
        return ApplicationError.NotFound(
            "social-publishing.publication.not-found",
            $"La publication sociale '{id}' est introuvable.");
    }

    public static ApplicationError PublicationCannotBeRetried()
    {
        return ApplicationError.RuleViolation(
            "social-publishing.publication.retry-not-allowed",
            "Seule une publication en échec peut être relancée.");
    }

    public static ApplicationError PublicationCannotBeManaged()
    {
        return ApplicationError.RuleViolation(
            "social-publishing.publication.manage-not-allowed",
            "Seule une publication encore présente sur le réseau social peut être modifiée ou supprimée.");
    }

    public static ApplicationError PublisherOperationFailed(string? message)
    {
        return ApplicationError.RuleViolation(
            "social-publishing.publisher.operation-failed",
            string.IsNullOrWhiteSpace(message)
                ? "Le réseau social n'a pas pu appliquer la modification."
                : message);
    }
}

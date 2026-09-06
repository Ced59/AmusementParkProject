using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Sharing;

public static class SharingApplicationErrors
{
    public static ApplicationError InvalidPublicationType()
    {
        return ApplicationError.Validation(
            "share-publication.type-invalid",
            "Le type de partage est invalide.");
    }

    public static ApplicationError InvalidContentPolicy(string code)
    {
        return ApplicationError.Validation(
            code,
            "La sélection des informations publiques est invalide.");
    }

    public static ApplicationError PreviewTypeNotAvailable()
    {
        return ApplicationError.RuleViolation(
            "share-publication.preview-type-not-available",
            "L’aperçu de ce type de partage n’est pas encore disponible.");
    }

    public static ApplicationError InvalidSource()
    {
        return ApplicationError.Validation(
            "share-publication.source-invalid",
            "La source sélectionnée ne correspond pas à ce partage.");
    }

    public static ApplicationError SourceUnavailable()
    {
        return ApplicationError.NotFound(
            "share-publication.source-unavailable",
            "La source de ce partage est indisponible.");
    }

    public static ApplicationError SourceChangedDuringPreview()
    {
        return ApplicationError.Conflict(
            "share-publication.source-changed",
            "Les données ont changé pendant la préparation de l’aperçu. Réessaie.");
    }

    public static ApplicationError SourceVersionUnavailable()
    {
        return ApplicationError.Conflict(
            "share-publication.source-version-unavailable",
            "La version des données partageables ne peut pas être calculée.");
    }

    public static ApplicationError PublicationChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "share-publication.concurrent-modification",
            "Le partage a été modifié simultanément. Réessaie.");
    }
}

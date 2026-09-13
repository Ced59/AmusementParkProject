using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Sharing;

public static class SharingApplicationErrors
{
    public const string SourceChangedCode = "share-publication.source-changed";

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
            SourceChangedCode,
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

    public static ApplicationError ApprovedPreviewExpired()
    {
        return ApplicationError.Conflict(
            "share-publication.preview-expired",
            "Les données ont changé depuis l’aperçu. Vérifie le nouvel aperçu avant de publier.");
    }

    public static ApplicationError PreviewApprovalRequired()
    {
        return ApplicationError.RuleViolation(
            "share-publication.preview-required",
            "Vérifie et approuve l’aperçu exact avant de publier.");
    }

    public static ApplicationError PreviewApprovalInvalid()
    {
        return ApplicationError.Conflict(
            "share-publication.preview-approval-invalid",
            "L’approbation ne correspond pas à l’aperçu affiché. Prépare un nouvel aperçu.");
    }

    public static ApplicationError RequiredPublicContentMissing()
    {
        return ApplicationError.Validation(
            "share-publication.required-content-missing",
            "La sélection ne contient pas les informations indispensables à ce partage.");
    }

    public static ApplicationError PublicContentNotSupported()
    {
        return ApplicationError.Validation(
            "share-publication.content-not-supported",
            "Une information sélectionnée n’est pas encore disponible sur cette page publique.");
    }

    public static ApplicationError InvalidVisitRecapSelection()
    {
        return ApplicationError.Validation(
            "share-publication.visit-recap-selection-invalid",
            "La sélection du récapitulatif de visite est invalide.");
    }

    public static ApplicationError VisitRecapTooLarge()
    {
        return ApplicationError.RuleViolation(
            "share-publication.visit-recap-too-large",
            "Cette visite contient trop d’entrées pour produire un partage fiable.");
    }

    public static ApplicationError InvalidYearRecapSelection()
    {
        return ApplicationError.Validation(
            "share-publication.year-recap-selection-invalid",
            "La sélection du bilan annuel est invalide.");
    }

    public static ApplicationError EmptyYearRecap()
    {
        return ApplicationError.RuleViolation(
            "share-publication.year-recap-empty",
            "Cette année ne contient aucune visite terminée à partager.");
    }

    public static ApplicationError InvalidPassportProfileSelection()
    {
        return ApplicationError.Validation(
            "share-publication.passport-profile-selection-invalid",
            "La sélection du passeport public est invalide.");
    }

    public static ApplicationError EmptyPassportProfile()
    {
        return ApplicationError.RuleViolation(
            "share-publication.passport-profile-empty",
            "Ce passeport ne contient aucune donnée publique à partager.");
    }

    public static ApplicationError SnapshotUnavailable()
    {
        return ApplicationError.NotFound(
            "share-publication.snapshot-unavailable",
            "Ce partage n’est plus disponible.");
    }

    public static ApplicationError SharedPublicationNotFound()
    {
        return ApplicationError.NotFound(
            "share-publication.not-found",
            "Ce partage est introuvable ou n’est plus public.");
    }

    public static ApplicationError PublicationNotRotatable()
    {
        return ApplicationError.RuleViolation(
            "share-publication.rotation-unavailable",
            "Seul un lien public actif peut être remplacé.");
    }

    public static ApplicationError InvalidSocialImageLanguage()
    {
        return ApplicationError.Validation(
            "share-publication.social-image-language-invalid",
            "La langue demandée pour l’image sociale n’est pas prise en charge.");
    }

    public static ApplicationError SocialImageNotAvailable()
    {
        return ApplicationError.NotFound(
            "share-publication.social-image-not-found",
            "Cette image sociale n’est plus disponible.");
    }

    public static ApplicationError InvalidComparisonCategories()
    {
        return ApplicationError.Validation(
            "profile-comparison.categories-invalid",
            "Choisis au moins une rubrique comparable de ton passeport public.");
    }

    public static ApplicationError ComparisonPassportUnavailable()
    {
        return ApplicationError.RuleViolation(
            "profile-comparison.passport-unavailable",
            "Ton passeport doit être public et autoriser les comparaisons.");
    }

    public static ApplicationError ComparisonCategoriesUnavailable()
    {
        return ApplicationError.RuleViolation(
            "profile-comparison.categories-unavailable",
            "Ton passeport public ne partage pas toutes les rubriques sélectionnées.");
    }

    public static ApplicationError ComparisonInvitationNotFound()
    {
        return ApplicationError.NotFound(
            "profile-comparison.invitation-not-found",
            "Cette invitation est introuvable ou n’est plus disponible.");
    }

    public static ApplicationError ComparisonInvitationNotAcceptable()
    {
        return ApplicationError.RuleViolation(
            "profile-comparison.invitation-not-acceptable",
            "Cette invitation ne peut pas être acceptée dans son état actuel.");
    }

    public static ApplicationError ComparisonInvitationChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "profile-comparison.invitation-concurrent-modification",
            "Cette invitation vient d’être utilisée. Recharge la page.");
    }

    public static ApplicationError ComparisonInvitationUnavailable()
    {
        return ApplicationError.Technical(
            "profile-comparison.invitation-unavailable",
            "L’invitation n’a pas pu être créée. Réessaie.");
    }

    public static ApplicationError ComparisonPassportsChanged()
    {
        return ApplicationError.RuleViolation(
            "profile-comparison.passports-changed",
            "Un des passeports publics a changé. Crée une nouvelle invitation.");
    }

    public static ApplicationError ComparisonUnavailable()
    {
        return ApplicationError.Technical(
            "profile-comparison.unavailable",
            "La comparaison n’a pas pu être préparée. Réessaie.");
    }

    public static ApplicationError ComparisonNotFound()
    {
        return ApplicationError.NotFound(
            "profile-comparison.not-found",
            "Cette comparaison est introuvable ou n’est plus disponible.");
    }

    public static ApplicationError ComparisonChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "profile-comparison.concurrent-modification",
            "Cette comparaison vient d’être modifiée. Recharge la page.");
    }
}

using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.LocalizedContent;

/// <summary>
/// Erreurs applicatives de la fonctionnalité d'administration des contenus localisés.
/// </summary>
public static class LocalizedContentApplicationErrors
{
    public static ApplicationError InvalidEntityType(string entityType)
    {
        return ApplicationError.Validation(
            "localized-content.entity-type.invalid",
            $"Le type d'entité localisable '{entityType}' n'est pas supporté.");
    }

    public static ApplicationError InvalidJson()
    {
        return ApplicationError.Validation(
            "localized-content.json.invalid",
            "Le JSON de localisation est invalide ou vide.");
    }

    public static ApplicationError UnsupportedField(LocalizedContentEntityType entityType, string fieldName)
    {
        return ApplicationError.Validation(
            "localized-content.field.unsupported",
            $"Le champ localisable '{fieldName}' n'est pas supporté pour le type '{entityType}'.");
    }

    public static ApplicationError EmptyLocalizationField(string fieldName)
    {
        return ApplicationError.Validation(
            "localized-content.field.empty",
            $"Le champ localisable '{fieldName}' ne contient aucune valeur valide.");
    }

    public static ApplicationError AccessConditionNotFound(string selector)
    {
        return ApplicationError.NotFound(
            "localized-content.access-condition.not-found",
            $"Aucune condition d'accès ne correspond au sélecteur '{selector}'.");
    }
}

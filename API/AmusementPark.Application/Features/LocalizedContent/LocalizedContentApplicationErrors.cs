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

    public static ApplicationError AccessConditionAmbiguous(string selector)
    {
        return ApplicationError.Validation(
            "localized-content.access-condition.ambiguous",
            $"Plusieurs conditions d'accès correspondent au sélecteur '{selector}'. Ajoute displayOrder ou typeKey pour lever l'ambiguïté.");
    }

    public static ApplicationError AccessConditionInvalid(string selector)
    {
        return ApplicationError.Validation(
            "localized-content.access-condition.invalid",
            $"La condition d'accès '{selector}' est invalide. Fournis au moins un type connu, un typeKey ou un displayOrder ciblant une condition existante.");
    }

    public static ApplicationError AccessConditionsRequireAttraction()
    {
        return ApplicationError.Validation(
            "localized-content.access-condition.requires-attraction",
            "Les conditions d'accès ne peuvent être appliquées qu'à un élément de parc de catégorie attraction.");
    }
}

using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.AttractionAccessConditionTypes;

/// <summary>
/// Erreurs applicatives du catalogue des types de conditions d'accès.
/// </summary>
public static class AttractionAccessConditionTypeApplicationErrors
{
    public static ApplicationError InvalidKey()
    {
        return ApplicationError.Validation(
            "attraction-access-condition-type.key.invalid",
            "La clé du type de condition d'accès est obligatoire.");
    }

    public static ApplicationError MissingLabels()
    {
        return ApplicationError.Validation(
            "attraction-access-condition-type.labels.missing",
            "Au moins un libellé localisé est requis pour créer un type de condition d'accès.");
    }
}

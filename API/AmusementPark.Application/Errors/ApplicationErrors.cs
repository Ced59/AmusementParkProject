namespace AmusementPark.Application.Errors;

/// <summary>
/// Fabrique centralisée d'erreurs applicatives communes.
/// </summary>
public static class ApplicationErrors
{
    /// <summary>
    /// Crée une erreur de champ requis.
    /// </summary>
    public static ApplicationError Required(string fieldName)
    {
        return ApplicationError.Validation(
            "validation.required",
            $"Le champ '{fieldName}' est requis.",
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                [fieldName] = new[] { "required" },
            });
    }

    /// <summary>
    /// Crée une erreur de pagination invalide.
    /// </summary>
    public static ApplicationError InvalidPagination()
    {
        return ApplicationError.Validation(
            "validation.pagination.invalid",
            "Les paramètres de pagination sont invalides.");
    }

    /// <summary>
    /// Crée une erreur de ressource absente.
    /// </summary>
    public static ApplicationError EntityNotFound(string entityName, string id)
    {
        return ApplicationError.NotFound(
            $"{entityName.ToLowerInvariant()}.not-found",
            $"La ressource '{entityName}' d'identifiant '{id}' est introuvable.");
    }

    /// <summary>
    /// Crée une erreur de conflit sur une ressource déjà existante.
    /// </summary>
    public static ApplicationError AlreadyExists(string entityName, string key)
    {
        return ApplicationError.Conflict(
            $"{entityName.ToLowerInvariant()}.already-exists",
            $"La ressource '{entityName}' existe déjà pour la clé '{key}'.");
    }
}

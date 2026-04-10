using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.DataSources;

/// <summary>
/// Fabrique d'erreurs de la feature DataSources.
/// </summary>
public static class DataSourcesApplicationErrors
{
    public static ApplicationError UnsupportedSource(string sourceKey)
    {
        return ApplicationError.NotFound(
            "data-sources.source.not-found",
            $"La source externe '{sourceKey}' est inconnue.");
    }

    public static ApplicationError ImportAlreadyRunning(string sourceKey)
    {
        return ApplicationError.Conflict(
            "data-sources.import.already-running",
            $"Un import est déjà en cours pour la source '{sourceKey}'.");
    }

    public static ApplicationError InvalidImport(string sourceKey, string message)
    {
        return ApplicationError.Validation(
            "data-sources.import.invalid",
            $"Import invalide pour la source '{sourceKey}' : {message}");
    }
}

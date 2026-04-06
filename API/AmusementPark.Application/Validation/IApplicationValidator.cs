namespace AmusementPark.Application.Validation;

/// <summary>
/// Contrat simple de validation applicative d'une requête ou commande.
/// </summary>
/// <typeparam name="TRequest">Type de requête à valider.</typeparam>
public interface IApplicationValidator<in TRequest>
{
    /// <summary>
    /// Valide la requête fournie.
    /// </summary>
    /// <param name="request">Requête à valider.</param>
    /// <returns>Liste d'erreurs détectées.</returns>
    IReadOnlyCollection<Errors.ApplicationError> Validate(TRequest request);
}

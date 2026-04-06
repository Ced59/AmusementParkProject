namespace AmusementPark.Application.Abstractions;

/// <summary>
/// Contrat générique de traitement d'une requête applicative.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Exécute la requête applicative.
    /// </summary>
    /// <param name="query">Requête à exécuter.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Réponse applicative.</returns>
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}

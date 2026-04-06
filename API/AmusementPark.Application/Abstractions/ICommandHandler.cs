namespace AmusementPark.Application.Abstractions;

/// <summary>
/// Contrat générique de traitement d'une commande applicative.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Exécute la commande applicative.
    /// </summary>
    /// <param name="command">Commande à exécuter.</param>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>Réponse applicative.</returns>
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

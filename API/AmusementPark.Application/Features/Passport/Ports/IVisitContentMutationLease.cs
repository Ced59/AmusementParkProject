using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Verrou distribué court qui sérialise le contenu d'une visite avec son cycle de vie.
/// </summary>
public interface IVisitContentMutationLease : IAsyncDisposable
{
    string Token { get; }

    /// <summary>
    /// Génération persistée que chaque écriture de contenu doit vérifier dans MongoDB.
    /// </summary>
    long ContentFenceToken { get; }

    /// <summary>
    /// Annule le travail protégé si le détenteur ne peut plus renouveler son token exact.
    /// </summary>
    CancellationToken LeaseLostToken { get; }

    /// <summary>
    /// Confirme que toutes les écritures protégées ont obtenu une réponse définitive.
    /// Une portée libérée sans cette confirmation force une récupération clôturée avant
    /// la mutation suivante afin de neutraliser toute écriture MongoDB tardive.
    /// </summary>
    void MarkMutationCompleted();
}

public interface IVisitContentMutationLeaseManager
{
    Task<IVisitContentMutationLease?> TryAcquireAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        CancellationToken cancellationToken);
}

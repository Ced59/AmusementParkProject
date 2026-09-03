using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Verrou distribué court qui sérialise le contenu d'une visite avec son cycle de vie.
/// </summary>
public interface IVisitContentMutationLease : IAsyncDisposable
{
    string Token { get; }

    /// <summary>
    /// Annule le travail protégé si le détenteur ne peut plus renouveler son token exact.
    /// </summary>
    CancellationToken LeaseLostToken { get; }
}

public interface IVisitContentMutationLeaseManager
{
    Task<IVisitContentMutationLease?> TryAcquireAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        CancellationToken cancellationToken);
}

using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Verrou distribué court qui sérialise le contenu d'une visite avec son cycle de vie.
/// </summary>
public interface IVisitContentMutationLease : IAsyncDisposable
{
}

public interface IVisitContentMutationLeaseManager
{
    Task<IVisitContentMutationLease?> TryAcquireAsync(
        Visit visit,
        DateTime acquiredAtUtc,
        CancellationToken cancellationToken);
}

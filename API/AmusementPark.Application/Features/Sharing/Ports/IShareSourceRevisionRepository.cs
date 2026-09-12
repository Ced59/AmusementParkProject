using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Ports;

public interface IShareSourceRevisionRepository
{
    Task<ShareSourceMutationLease?> TryBeginMutationAsync(
        string scopeKey,
        CancellationToken cancellationToken);

    Task<ShareSourceMutationLease> BeginMutationAsync(
        string scopeKey,
        CancellationToken cancellationToken);

    Task<ShareSourceRevision> CompleteMutationAsync(
        ShareSourceMutationLease mutationLease,
        bool sourceChanged,
        CancellationToken cancellationToken);

    Task<ShareSourceRevision> GetOrCreateAsync(
        string scopeKey,
        CancellationToken cancellationToken);

    Task EnsureCreatedAsync(
        IReadOnlyCollection<string> scopeKeys,
        CancellationToken cancellationToken);

    Task<ShareSourceRevision> ReconcileFingerprintAsync(
        string scopeKey,
        string sourceFingerprint,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, ShareSourceRevision>> GetSnapshotAsync(
        IReadOnlyCollection<string> scopeKeys,
        CancellationToken cancellationToken);
}

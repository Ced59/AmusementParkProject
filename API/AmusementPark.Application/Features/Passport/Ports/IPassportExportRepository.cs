using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

public interface IPassportExportRepository
{
    Task CreateAsync(PassportExport passportExport, CancellationToken cancellationToken);

    Task<PassportExport?> GetOwnedAsync(
        string exportId,
        string userId,
        CancellationToken cancellationToken);

    Task<bool> TryMarkProcessingAsync(
        string exportId,
        string userId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<bool> TryCompleteAsync(
        string exportId,
        string userId,
        PassportExportArtifact artifact,
        DateTime completedAtUtc,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken);

    Task<bool> TryFailAsync(
        string exportId,
        string userId,
        string errorCode,
        DateTime failedAtUtc,
        CancellationToken cancellationToken);

    Task<PassportExportDownload?> GetOwnedDownloadAsync(
        string exportId,
        string userId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<PassportExport>> ListPendingForReconciliationAsync(
        DateTime maximumUpdatedAtUtc,
        DateTime minimumExpiresAtUtc,
        int maximumCount,
        CancellationToken cancellationToken);
}

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Coordonne les générations paresseuses de variantes avec les suppressions d'images.
/// </summary>
public interface IImageVariantGenerationLease
{
    Task<bool> TryAcquireAsync(
        string pathWithoutExtension,
        string leaseToken,
        DateTime acquiredAtUtc,
        DateTime leaseUntilUtc,
        CancellationToken cancellationToken);

    Task<bool> RenewAsync(
        string pathWithoutExtension,
        string leaseToken,
        DateTime renewedAtUtc,
        DateTime leaseUntilUtc,
        CancellationToken cancellationToken);

    Task ReleaseAsync(
        string pathWithoutExtension,
        string leaseToken,
        CancellationToken cancellationToken);
}

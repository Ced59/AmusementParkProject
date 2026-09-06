using AmusementPark.Core.Domain.Images;

namespace AmusementPark.Application.Features.Images.Ports;

/// <summary>
/// Serializes mutations of the current image within an ownership scope.
/// </summary>
public interface IImageCurrentMutationLock
{
    Task<TResult> ExecuteAsync<TResult>(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken);

    Task<TResult> ExecuteAsync<TResult>(
        ImageOwnerType ownerType,
        string ownerId,
        ImageCategory category,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken,
        CancellationToken consistencyCancellationToken);
}

using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IUserCollectionEntryRepository
{
    Task<IReadOnlyCollection<UserCollectionEntry>> ListOwnedAsync(
        string userId,
        CollectionTargetType? targetType,
        string? targetId,
        CancellationToken cancellationToken);

    Task<UserCollectionEntry?> GetOwnedByIdentityAsync(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CancellationToken cancellationToken);

    Task<UserCollectionWriteOutcome> CreateAsync(
        UserCollectionEntry entry,
        CancellationToken cancellationToken);

    Task<bool> TrySynchronizeTargetStatusesAsync(
        IReadOnlyCollection<UserCollectionEntry> entries,
        CancellationToken cancellationToken);

    Task DeleteOwnedByIdentityAsync(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind,
        CancellationToken cancellationToken);
}

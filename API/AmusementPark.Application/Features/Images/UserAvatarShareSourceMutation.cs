using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Images;

internal static class UserAvatarShareSourceMutation
{
    public static IReadOnlyCollection<string> ResolveImpactedOwnerUserIds(
        Image? previousImage,
        ImageOwnerType nextOwnerType,
        string? nextOwnerId,
        ImageCategory nextCategory)
    {
        HashSet<string> ownerUserIds = new HashSet<string>(StringComparer.Ordinal);
        AddAvatarOwner(
            ownerUserIds,
            previousImage?.OwnerType ?? ImageOwnerType.None,
            previousImage?.OwnerId,
            previousImage?.Category ?? ImageCategory.Avatar);
        AddAvatarOwner(
            ownerUserIds,
            nextOwnerType,
            nextOwnerId,
            nextCategory);
        return ownerUserIds.ToArray();
    }

    public static async Task<UserAvatarShareSourceMutationContext> BeginAsync(
        IReadOnlyCollection<string> ownerUserIds,
        IPersonalRankingShareSourceRevisionGuard revisionGuard,
        IImageRepository imageRepository,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string?> publicAvatarUrlsBefore =
            await ReadPublicAvatarUrlsAsync(
                ownerUserIds,
                imageRepository,
                cancellationToken);
        Dictionary<string, ShareSourceMutationLease> leases =
            new Dictionary<string, ShareSourceMutationLease>(StringComparer.Ordinal);
        try
        {
            foreach (string ownerUserId in ownerUserIds)
            {
                leases[ownerUserId] = await revisionGuard.BeginAvatarMutationAsync(
                    ownerUserId,
                    cancellationToken);
            }

            return new UserAvatarShareSourceMutationContext(
                leases,
                publicAvatarUrlsBefore);
        }
        catch
        {
            foreach (ShareSourceMutationLease lease in leases.Values)
            {
                await revisionGuard.CompleteMutationAsync(
                    lease,
                    false,
                    CancellationToken.None);
            }

            throw;
        }
    }

    public static async Task SynchronizeAsync(
        IEnumerable<string> ownerUserIds,
        IImageRepository imageRepository,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        foreach (string ownerUserId in ownerUserIds)
        {
            User? user = await userRepository.GetByIdAsync(ownerUserId, cancellationToken);
            if (user is null)
            {
                continue;
            }

            Image? currentAvatar = await imageRepository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                ownerUserId,
                ImageCategory.Avatar,
                cancellationToken);
            string? avatarUrl = currentAvatar is not null && currentAvatar.IsPublished
                ? $"/images/{currentAvatar.Id}"
                : null;
            if (string.Equals(user.AvatarUrl, avatarUrl, StringComparison.Ordinal))
            {
                continue;
            }

            bool updated = await userRepository.UpdateAvatarUrlAsync(
                user.Id,
                avatarUrl,
                cancellationToken);
            if (!updated)
            {
                throw new InvalidOperationException("Unable to synchronize the public user avatar.");
            }
        }
    }

    public static async Task CompleteAsync(
        UserAvatarShareSourceMutationContext context,
        IImageRepository imageRepository,
        IPersonalRankingShareSourceRevisionGuard revisionGuard)
    {
        IReadOnlyDictionary<string, string?> publicAvatarUrlsAfter =
            await ReadPublicAvatarUrlsAsync(
                context.Leases.Keys.ToArray(),
                imageRepository,
                CancellationToken.None);
        foreach (KeyValuePair<string, ShareSourceMutationLease> pair in context.Leases)
        {
            context.PublicAvatarUrlsBefore.TryGetValue(
                pair.Key,
                out string? publicAvatarUrlBefore);
            publicAvatarUrlsAfter.TryGetValue(
                pair.Key,
                out string? publicAvatarUrlAfter);
            await revisionGuard.CompleteMutationAsync(
                pair.Value,
                !string.Equals(
                    publicAvatarUrlBefore,
                    publicAvatarUrlAfter,
                    StringComparison.Ordinal),
                CancellationToken.None);
        }
    }

    private static async Task<IReadOnlyDictionary<string, string?>> ReadPublicAvatarUrlsAsync(
        IReadOnlyCollection<string> ownerUserIds,
        IImageRepository imageRepository,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string?> publicAvatarUrls =
            new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (string ownerUserId in ownerUserIds
                     .Select(static value => value.Trim())
                     .Where(static value => value.Length > 0)
                     .Distinct(StringComparer.Ordinal))
        {
            Image? currentAvatar = await imageRepository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                ownerUserId,
                ImageCategory.Avatar,
                cancellationToken);
            publicAvatarUrls[ownerUserId] = currentAvatar is not null
                && currentAvatar.IsPublished
                ? $"/images/{currentAvatar.Id}"
                : null;
        }

        return publicAvatarUrls;
    }

    private static void AddAvatarOwner(
        ISet<string> ownerUserIds,
        ImageOwnerType ownerType,
        string? ownerId,
        ImageCategory category)
    {
        if (ownerType == ImageOwnerType.User
            && category == ImageCategory.Avatar
            && !string.IsNullOrWhiteSpace(ownerId))
        {
            ownerUserIds.Add(ownerId.Trim());
        }
    }
}

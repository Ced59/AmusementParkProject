using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Images;

internal sealed record UserAvatarShareSourceMutationContext(
    IReadOnlyDictionary<string, ShareSourceMutationLease> Leases,
    IReadOnlyDictionary<string, string?> PublicAvatarUrlsBefore);

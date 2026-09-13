using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record SharePublicationCacheInvalidationJobPayload(
    string PublicationId,
    string OwnerUserId,
    SharePublicationType PublicationType,
    long MinimumPublicationStateVersion,
    IReadOnlyCollection<string> ShareIds);

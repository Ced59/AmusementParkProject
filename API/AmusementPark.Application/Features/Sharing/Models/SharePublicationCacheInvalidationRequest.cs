using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record SharePublicationCacheInvalidationRequest(
    string PublicationId,
    SharePublicationType PublicationType,
    IReadOnlyCollection<string> ShareIds);

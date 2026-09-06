using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ResolvedSharePublicationResult(
    string OwnerUserId,
    string DisplayName,
    SharePublicationType PublicationType,
    ShareContentPolicy ContentPolicy,
    DateTime PublishedAtUtc,
    string SourceScopeKey,
    long SourceVersion,
    long PublicationVersion);

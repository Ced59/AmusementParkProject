using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record SharePublicationSourceVersionRequest(
    string SourceScopeKey,
    ShareContentPolicy ContentPolicy,
    SharePublicationId? PublicationId = null,
    long? PublicationVersion = null,
    PassportProfileShareInput? PassportProfile = null);

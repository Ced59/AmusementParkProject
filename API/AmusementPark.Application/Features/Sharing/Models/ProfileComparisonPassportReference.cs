using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ProfileComparisonPassportReference(
    SharePublicationId PublicationId,
    long PublicationVersion,
    string? DisplayName,
    ShareContentPolicy ContentPolicy,
    PassportProfileShareSnapshot Snapshot);

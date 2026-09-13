using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record SharePublicationSnapshotCloneRequest(
    SharePublicationId PublicationId,
    long SourcePublicationVersion,
    long TargetPublicationVersion,
    long PublicationStateVersion,
    long SourceVersion,
    ShareContentPolicy ContentPolicy,
    string ContentFingerprint);

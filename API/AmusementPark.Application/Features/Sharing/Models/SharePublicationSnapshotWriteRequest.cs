using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record SharePublicationSnapshotWriteRequest(
    SharePublicationId PublicationId,
    long PublicationVersion,
    long PublicationStateVersion,
    string OwnerUserId,
    string SourceId,
    long SourceVersion,
    ShareContentPolicy ContentPolicy,
    string ContentFingerprint,
    VisitRecapShareInput? VisitRecap = null,
    YearRecapShareInput? YearRecap = null);

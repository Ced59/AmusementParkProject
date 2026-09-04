using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionTombstoneRequest(
    VisitId VisitId,
    string UserId,
    long ExpectedVersion,
    string ClientOperationId,
    DateTime DeletedAtUtc,
    DateTime PurgeScheduledForUtc,
    string? ContentMutationLeaseToken,
    PassportAuditEvent AuditEvent);

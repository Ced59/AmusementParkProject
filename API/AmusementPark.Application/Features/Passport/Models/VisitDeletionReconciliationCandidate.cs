using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionReconciliationCandidate(
    VisitId VisitId,
    string UserId,
    long DeletionVersion,
    DateTime DeletedAtUtc,
    DateTime PurgeScheduledForUtc,
    bool IsExportInvalidationEnsured,
    bool IsPurgeJobEnsured);

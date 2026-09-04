using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionPurgeCandidate(
    VisitId VisitId,
    string UserId,
    long DeletionVersion,
    DateTime PurgeScheduledForUtc);

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionReceipt(
    string VisitId,
    DateTime DeletedAtUtc,
    DateTime PurgeScheduledForUtc,
    long DeletionVersion,
    bool WasReplayed);

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionReceipt(
    string VisitId,
    DateTime DeletedAtUtc,
    DateTime PurgeScheduledForUtc,
    bool WasReplayed);

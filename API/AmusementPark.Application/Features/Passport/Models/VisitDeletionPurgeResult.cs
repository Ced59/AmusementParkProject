namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionPurgeResult(
    bool IsCompleted,
    int DeletedDocumentCount);

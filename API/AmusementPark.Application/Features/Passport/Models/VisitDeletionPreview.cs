namespace AmusementPark.Application.Features.Passport.Models;

public sealed record VisitDeletionPreview(
    string VisitId,
    long ExpectedVersion,
    long OccurrenceCount,
    long AssessmentCount,
    int RetentionDays);

using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed record RideOccurrenceAuditSnapshot(
    RideOccurrenceStatus Status,
    OccurrenceMoment Moment,
    HistoricalConsistency HistoricalConsistency,
    HistoricalTargetReference? HistoricalTarget,
    string? PrivateNote,
    long SortPosition,
    byte? AssessmentValueHalfSteps,
    string? AssessmentPrivateComment,
    int? AssessmentRevision)
{
    public static RideOccurrenceAuditSnapshot Capture(RideOccurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);
        return new RideOccurrenceAuditSnapshot(
            occurrence.Status,
            occurrence.Moment,
            occurrence.HistoricalConsistency,
            occurrence.HistoricalTarget,
            occurrence.PrivateNote,
            occurrence.SortPosition,
            occurrence.Assessment?.Value.HalfSteps,
            occurrence.Assessment?.PrivateComment,
            occurrence.Assessment?.Revision);
    }
}

namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalVisitDiagnosticCounts
{
    public HistoricalVisitDiagnosticCounts(
        int potentiallyInconsistentVisitCount,
        int confirmedConflictVisitCount,
        int unverifiedVisitCount)
    {
        if (potentiallyInconsistentVisitCount < 0
            || confirmedConflictVisitCount < 0
            || unverifiedVisitCount < 0
            || confirmedConflictVisitCount > potentiallyInconsistentVisitCount
            || unverifiedVisitCount > potentiallyInconsistentVisitCount)
        {
            throw new ArgumentException("The historical visit diagnostic counts are invalid.");
        }

        this.PotentiallyInconsistentVisitCount = potentiallyInconsistentVisitCount;
        this.ConfirmedConflictVisitCount = confirmedConflictVisitCount;
        this.UnverifiedVisitCount = unverifiedVisitCount;
    }

    public int PotentiallyInconsistentVisitCount { get; }

    public int ConfirmedConflictVisitCount { get; }

    public int UnverifiedVisitCount { get; }
}

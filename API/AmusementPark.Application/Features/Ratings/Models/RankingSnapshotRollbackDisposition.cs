namespace AmusementPark.Application.Features.Ratings.Models;

public enum RankingSnapshotRollbackDisposition
{
    RolledBack,
    AlreadyRolledBack,
    Missing,
    InvalidPreviousSnapshot,
    ConcurrencyConflict,
}

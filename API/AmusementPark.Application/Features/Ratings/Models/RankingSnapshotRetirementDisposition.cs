namespace AmusementPark.Application.Features.Ratings.Models;

public enum RankingSnapshotRetirementDisposition
{
    Retired,
    AlreadyUnavailable,
    Stale,
    ConcurrencyConflict,
}

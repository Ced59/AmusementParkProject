namespace AmusementPark.Application.Features.Ratings.Models;

public enum RankingSnapshotPublicationDisposition
{
    Published,
    AlreadyPublished,
    Stale,
    Missing,
    InvalidSnapshot,
    ConcurrencyConflict,
}

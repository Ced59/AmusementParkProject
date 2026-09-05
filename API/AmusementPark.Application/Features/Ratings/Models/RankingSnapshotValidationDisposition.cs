namespace AmusementPark.Application.Features.Ratings.Models;

public enum RankingSnapshotValidationDisposition
{
    Validated,
    AlreadyValidated,
    Failed,
    Missing,
    BuildNotValidatable,
    ConcurrencyConflict,
}

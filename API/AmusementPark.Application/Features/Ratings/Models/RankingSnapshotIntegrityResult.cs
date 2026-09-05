namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotIntegrityResult(bool IsValid, string? ErrorCode)
{
    public static RankingSnapshotIntegrityResult Valid { get; } = new RankingSnapshotIntegrityResult(true, null);

    public static RankingSnapshotIntegrityResult Invalid(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        return new RankingSnapshotIntegrityResult(false, errorCode);
    }
}

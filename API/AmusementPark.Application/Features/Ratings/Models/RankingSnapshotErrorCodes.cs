namespace AmusementPark.Application.Features.Ratings.Models;

public static class RankingSnapshotErrorCodes
{
    public const string ChunkCountMismatch = "ranking-snapshot.chunk-count-mismatch";
    public const string ChunkIndexMismatch = "ranking-snapshot.chunk-index-mismatch";
    public const string BuildAttemptMismatch = "ranking-snapshot.build-attempt-mismatch";
    public const string ChunkSizeInvalid = "ranking-snapshot.chunk-size-invalid";
    public const string PositionSequenceInvalid = "ranking-snapshot.position-sequence-invalid";
    public const string RankSequenceInvalid = "ranking-snapshot.rank-sequence-invalid";
    public const string ScoreOrderInvalid = "ranking-snapshot.score-order-invalid";
    public const string DuplicateTarget = "ranking-snapshot.duplicate-target";
    public const string TargetFamilyMismatch = "ranking-snapshot.target-family-mismatch";
    public const string ScopeFilterMismatch = "ranking-snapshot.scope-filter-mismatch";
    public const string MethodologyMismatch = "ranking-snapshot.methodology-mismatch";
    public const string ChunkChecksumMismatch = "ranking-snapshot.chunk-checksum-mismatch";
    public const string EntryCountMismatch = "ranking-snapshot.entry-count-mismatch";
    public const string SnapshotChecksumMismatch = "ranking-snapshot.checksum-mismatch";
    public const string BuildFailed = "ranking-snapshot.build-failed";
}

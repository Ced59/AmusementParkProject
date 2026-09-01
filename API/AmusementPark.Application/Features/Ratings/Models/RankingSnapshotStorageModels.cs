using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record StartRankingSnapshotBuildRequest(
    RankingScopeKey ScopeKey,
    RatingMethodologyVersion MethodologyVersion,
    long SourceRevision,
    int TotalEntryCount,
    int EligibleEntryCount,
    RankingSnapshotChecksum Checksum);

public enum RankingSnapshotBuildStartDisposition
{
    Created,
    Restarted,
    Existing,
    Conflict,
}

public sealed record RankingSnapshotBuildStartResult(
    RankingSnapshotBuildStartDisposition Disposition,
    RankingSnapshotHeader? Header);

public enum RankingSnapshotChunkWriteDisposition
{
    Written,
    AlreadyWritten,
    Conflict,
    BuildNotWritable,
}

public sealed record RankingSnapshotChunkWriteResult(
    RankingSnapshotChunkWriteDisposition Disposition);

public enum RankingSnapshotValidationDisposition
{
    Validated,
    AlreadyValidated,
    Failed,
    Missing,
    BuildNotValidatable,
    ConcurrencyConflict,
}

public sealed record RankingSnapshotValidationResult(
    RankingSnapshotValidationDisposition Disposition,
    RankingSnapshotHeader? Header,
    string? ErrorCode = null);

public enum RankingSnapshotPublicationDisposition
{
    Published,
    AlreadyPublished,
    Stale,
    Missing,
    InvalidSnapshot,
    ConcurrencyConflict,
}

public sealed record RankingSnapshotPublicationResult(
    RankingSnapshotPublicationDisposition Disposition,
    RankingPublicationPointer? Pointer);

public sealed record RetireRankingPublicationRequest(
    RankingScopeKey ScopeKey,
    RatingMethodologyVersion MethodologyVersion,
    long SourceRevision);

public enum RankingSnapshotRetirementDisposition
{
    Retired,
    AlreadyUnavailable,
    Stale,
    ConcurrencyConflict,
}

public sealed record RankingSnapshotRetirementResult(
    RankingSnapshotRetirementDisposition Disposition,
    RankingPublicationPointer? Pointer);

public sealed record RankingSnapshotRollbackRequest(
    RankingScopeKey ScopeKey,
    RankingSnapshotId ExpectedCurrentSnapshotId,
    RankingSnapshotId ExpectedPreviousSnapshotId,
    long ExpectedPointerVersion);

public enum RankingSnapshotRollbackDisposition
{
    RolledBack,
    AlreadyRolledBack,
    Missing,
    InvalidPreviousSnapshot,
    ConcurrencyConflict,
}

public sealed record RankingSnapshotRollbackResult(
    RankingSnapshotRollbackDisposition Disposition,
    RankingPublicationPointer? Pointer);

public sealed record RankingSnapshotPage(
    RankingSnapshotHeader Header,
    IReadOnlyCollection<RankingSnapshotEntry> Entries,
    int Offset,
    int Limit);

public sealed record RankingSnapshotIntegrityResult(bool IsValid, string? ErrorCode)
{
    public static RankingSnapshotIntegrityResult Valid { get; } = new RankingSnapshotIntegrityResult(true, null);

    public static RankingSnapshotIntegrityResult Invalid(string errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        return new RankingSnapshotIntegrityResult(false, errorCode);
    }
}

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

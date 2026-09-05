namespace AmusementPark.Application.Features.Ratings.Models;

public static class RatingRankingRebuildErrorCodes
{
    public const string InvalidPayload = "ranking-snapshot.invalid-rebuild-payload";
    public const string UnknownScope = "ranking-snapshot.unknown-scope";
    public const string SourceRevisionUnavailable = "ranking-snapshot.source-revision-unavailable";
    public const string SourceSetTruncated = "ranking-snapshot.source-set-truncated";
    public const string BelowMinimumEligibleEntries = "ranking-snapshot.below-minimum-eligible-entries";
    public const string BuildConflict = "ranking-snapshot.build-conflict";
    public const string ChunkWriteConflict = "ranking-snapshot.chunk-write-conflict";
    public const string ValidationFailed = "ranking-snapshot.validation-failed";
    public const string PublicationConflict = "ranking-snapshot.publication-conflict";
    public const string RetirementConflict = "ranking-snapshot.retirement-conflict";
    public const string CacheInvalidationFailed = "ranking-snapshot.cache-invalidation-failed";
}

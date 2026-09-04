using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// En-tête borné d'un build de classement. Le pointeur de publication reste la source de vérité publique.
/// </summary>
public sealed class RankingSnapshotHeader
{
    public const int MaximumCandidateEntryCount = 5000;

    public RankingSnapshotHeader(
        RankingSnapshotId id,
        RankingScopeKey scopeKey,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        RankingSnapshotStatus status,
        int totalEntryCount,
        int eligibleEntryCount,
        int chunkSize,
        int chunkCount,
        RankingSnapshotChecksum checksum,
        DateTime generatedAtUtc,
        DateTime? validatedAtUtc = null,
        DateTime? publishedAtUtc = null,
        string? failureCode = null,
        int buildAttempt = 1)
    {
        _ = id.Value;
        _ = scopeKey.Value;
        _ = methodologyVersion.Value;
        _ = checksum.Value;
        if (sourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (totalEntryCount < 0 || totalEntryCount > MaximumCandidateEntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(totalEntryCount));
        }

        if (eligibleEntryCount < 0 || eligibleEntryCount > totalEntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleEntryCount));
        }

        if (chunkSize < RankingScopeDefinition.MinimumPageSize ||
            chunkSize > RankingScopeDefinition.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize));
        }

        int expectedChunkCount = eligibleEntryCount == 0
            ? 0
            : ((eligibleEntryCount - 1) / chunkSize) + 1;
        if (chunkCount != expectedChunkCount)
        {
            throw new ArgumentException(
                "The chunk count must cover every eligible entry exactly once.",
                nameof(chunkCount));
        }

        if (buildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildAttempt));
        }

        ValidateUtc(generatedAtUtc, nameof(generatedAtUtc));
        ValidateOptionalUtc(validatedAtUtc, nameof(validatedAtUtc));
        ValidateOptionalUtc(publishedAtUtc, nameof(publishedAtUtc));
        string? normalizedFailureCode = string.IsNullOrWhiteSpace(failureCode)
            ? null
            : IdentifierRules.NormalizeRequired(failureCode, nameof(failureCode));
        ValidateLifecycle(status, validatedAtUtc, publishedAtUtc, normalizedFailureCode);

        this.Id = id;
        this.ScopeKey = scopeKey;
        this.MethodologyVersion = methodologyVersion;
        this.SourceRevision = sourceRevision;
        this.Status = status;
        this.TotalEntryCount = totalEntryCount;
        this.EligibleEntryCount = eligibleEntryCount;
        this.ChunkSize = chunkSize;
        this.ChunkCount = chunkCount;
        this.Checksum = checksum;
        this.GeneratedAtUtc = generatedAtUtc;
        this.ValidatedAtUtc = validatedAtUtc;
        this.PublishedAtUtc = publishedAtUtc;
        this.FailureCode = normalizedFailureCode;
        this.BuildAttempt = buildAttempt;
    }

    public RankingSnapshotId Id { get; }

    public RankingScopeKey ScopeKey { get; }

    public RatingMethodologyVersion MethodologyVersion { get; }

    public long SourceRevision { get; }

    public RankingSnapshotStatus Status { get; }

    public int TotalEntryCount { get; }

    public int EligibleEntryCount { get; }

    public int ChunkSize { get; }

    public int ChunkCount { get; }

    public RankingSnapshotChecksum Checksum { get; }

    public DateTime GeneratedAtUtc { get; }

    public DateTime? ValidatedAtUtc { get; }

    public DateTime? PublishedAtUtc { get; }

    public string? FailureCode { get; }

    public int BuildAttempt { get; }

    private static void ValidateLifecycle(
        RankingSnapshotStatus status,
        DateTime? validatedAtUtc,
        DateTime? publishedAtUtc,
        string? failureCode)
    {
        bool isValid = status switch
        {
            RankingSnapshotStatus.Building => !validatedAtUtc.HasValue
                && !publishedAtUtc.HasValue
                && failureCode is null,
            RankingSnapshotStatus.Validated => validatedAtUtc.HasValue
                && !publishedAtUtc.HasValue
                && failureCode is null,
            RankingSnapshotStatus.Current or RankingSnapshotStatus.Superseded => validatedAtUtc.HasValue
                && publishedAtUtc.HasValue
                && failureCode is null,
            RankingSnapshotStatus.Failed => !publishedAtUtc.HasValue && failureCode is not null,
            _ => false,
        };

        if (!isValid)
        {
            throw new ArgumentException("The ranking snapshot lifecycle metadata is inconsistent.", nameof(status));
        }
    }

    private static void ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", parameterName);
        }
    }

    private static void ValidateOptionalUtc(DateTime? value, string parameterName)
    {
        if (value.HasValue)
        {
            ValidateUtc(value.Value, parameterName);
        }
    }
}

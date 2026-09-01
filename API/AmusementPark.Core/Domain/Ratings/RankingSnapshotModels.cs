using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

public readonly record struct RankingSnapshotId
{
    private readonly string? value;

    private RankingSnapshotId(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized ranking snapshot identifier has no value.");

    public static RankingSnapshotId Parse(string? value)
    {
        return new RankingSnapshotId(IdentifierRules.NormalizeRequired(value, nameof(value)));
    }

    public override string ToString()
    {
        return this.Value;
    }
}

public readonly record struct RankingSnapshotChecksum
{
    public const int HexadecimalLength = 64;

    private readonly string? value;

    private RankingSnapshotChecksum(string value)
    {
        this.value = value;
    }

    public string Value => this.value
        ?? throw new InvalidOperationException("An uninitialized ranking snapshot checksum has no value.");

    public static RankingSnapshotChecksum Parse(string? value)
    {
        if (!TryParse(value, out RankingSnapshotChecksum checksum))
        {
            throw new ArgumentException(
                "A ranking snapshot checksum must contain exactly 64 lowercase hexadecimal characters.",
                nameof(value));
        }

        return checksum;
    }

    public static bool TryParse(string? value, out RankingSnapshotChecksum checksum)
    {
        checksum = default;
        if (value is null || value.Length != HexadecimalLength)
        {
            return false;
        }

        foreach (char character in value)
        {
            bool isLowercaseHexadecimalLetter = character is >= 'a' and <= 'f';
            bool isDigit = character is >= '0' and <= '9';
            if (!isLowercaseHexadecimalLetter && !isDigit)
            {
                return false;
            }
        }

        checksum = new RankingSnapshotChecksum(value);
        return true;
    }

    public override string ToString()
    {
        return this.Value;
    }
}

public enum RankingSnapshotStatus
{
    Building,
    Validated,
    Current,
    Superseded,
    Failed,
}

/// <summary>
/// Entrée éligible d'un classement matérialisé. Les cibles sans rang restent dans les agrégats sources.
/// </summary>
public sealed class RankingSnapshotEntry
{
    public RankingSnapshotEntry(
        int rank,
        RatingTargetType targetType,
        string targetId,
        double score,
        RankingEvidence evidence)
        : this(rank, rank, targetType, targetId, null, score, evidence)
    {
    }

    public RankingSnapshotEntry(
        int position,
        int rank,
        RatingTargetType targetType,
        string targetId,
        double score,
        RankingEvidence evidence)
        : this(position, rank, targetType, targetId, null, score, evidence)
    {
    }

    public RankingSnapshotEntry(
        int position,
        int rank,
        RatingTargetType targetType,
        string targetId,
        ParkItemCategory? parkItemCategory,
        double score,
        RankingEvidence evidence)
    {
        if (position <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (rank <= 0 || rank > position)
        {
            throw new ArgumentOutOfRangeException(nameof(rank));
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        if (targetType == RatingTargetType.ParkItem &&
            (!parkItemCategory.HasValue || !Enum.IsDefined(parkItemCategory.Value)))
        {
            throw new ArgumentException(
                "A park-item ranking entry must preserve its category.",
                nameof(parkItemCategory));
        }

        if (targetType == RatingTargetType.Park && parkItemCategory.HasValue)
        {
            throw new ArgumentException(
                "A park ranking entry cannot have a park-item category.",
                nameof(parkItemCategory));
        }

        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        if (!double.IsFinite(score) || score < 0d || score > 5d)
        {
            throw new ArgumentOutOfRangeException(nameof(score));
        }

        ArgumentNullException.ThrowIfNull(evidence);
        _ = evidence.MethodologyVersion.Value;
        if (!RankingEligibilityPolicy.TryResolve(
                evidence.MethodologyVersion,
                out RankingEligibilityPolicy? eligibilityPolicy) ||
            !eligibilityPolicy.IsEligibleSnapshotEvidence(targetType, evidence))
        {
            throw new ArgumentException(
                "A ranking snapshot can contain only evidence derived from its versioned policy.",
                nameof(evidence));
        }

        this.Position = position;
        this.Rank = rank;
        this.TargetType = targetType;
        this.TargetId = normalizedTargetId;
        this.ParkItemCategory = parkItemCategory;
        this.Score = score;
        this.Evidence = evidence;
    }

    public int Position { get; }

    public int Rank { get; }

    public RatingTargetType TargetType { get; }

    public string TargetId { get; }

    public ParkItemCategory? ParkItemCategory { get; }

    public double Score { get; }

    public RankingEvidence Evidence { get; }
}

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

public sealed class RankingSnapshotChunk
{
    public RankingSnapshotChunk(
        RankingSnapshotId snapshotId,
        int chunkIndex,
        IReadOnlyCollection<RankingSnapshotEntry> entries,
        RankingSnapshotChecksum checksum,
        int buildAttempt = 1)
    {
        _ = snapshotId.Value;
        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex));
        }

        ArgumentNullException.ThrowIfNull(entries);
        RankingSnapshotEntry[] materializedEntries = entries.ToArray();
        if (materializedEntries.Length == 0 ||
            materializedEntries.Length > RankingScopeDefinition.MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(entries));
        }

        if (buildAttempt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(buildAttempt));
        }

        for (int index = 0; index < materializedEntries.Length; index++)
        {
            ArgumentNullException.ThrowIfNull(materializedEntries[index]);
            if (index > 0 && materializedEntries[index].Position != materializedEntries[index - 1].Position + 1)
            {
                throw new ArgumentException("Chunk positions must be contiguous.", nameof(entries));
            }

            if (index > 0 && materializedEntries[index].Rank < materializedEntries[index - 1].Rank)
            {
                throw new ArgumentException("Public ranks must be ordered.", nameof(entries));
            }
        }

        _ = checksum.Value;
        this.SnapshotId = snapshotId;
        this.ChunkIndex = chunkIndex;
        this.Entries = Array.AsReadOnly(materializedEntries);
        this.Checksum = checksum;
        this.BuildAttempt = buildAttempt;
    }

    public RankingSnapshotId SnapshotId { get; }

    public int ChunkIndex { get; }

    public IReadOnlyCollection<RankingSnapshotEntry> Entries { get; }

    public int FirstPosition => this.Entries.First().Position;

    public int LastPosition => this.Entries.Last().Position;

    public int FirstRank => this.Entries.First().Rank;

    public int LastRank => this.Entries.Last().Rank;

    public RankingSnapshotChecksum Checksum { get; }

    public int BuildAttempt { get; }
}

public sealed class RankingPublicationPointer
{
    public RankingPublicationPointer(
        RankingScopeKey scopeKey,
        RankingSnapshotId currentSnapshotId,
        RankingSnapshotId? previousSnapshotId,
        DateTime? previousSnapshotPublishedAtUtc,
        RatingMethodologyVersion methodologyVersion,
        long sourceRevision,
        long highestPublishedSourceRevision,
        long version,
        DateTime updatedAtUtc)
    {
        _ = scopeKey.Value;
        _ = currentSnapshotId.Value;
        if (previousSnapshotId.HasValue)
        {
            _ = previousSnapshotId.Value.Value;
        }

        if (previousSnapshotId.HasValue != previousSnapshotPublishedAtUtc.HasValue)
        {
            throw new ArgumentException(
                "A previous snapshot and its publication timestamp must be provided together.",
                nameof(previousSnapshotPublishedAtUtc));
        }

        if (previousSnapshotPublishedAtUtc.HasValue &&
            previousSnapshotPublishedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The previous snapshot publication timestamp must use UTC.",
                nameof(previousSnapshotPublishedAtUtc));
        }

        _ = methodologyVersion.Value;
        if (sourceRevision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceRevision));
        }

        if (highestPublishedSourceRevision < sourceRevision)
        {
            throw new ArgumentOutOfRangeException(nameof(highestPublishedSourceRevision));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (updatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The timestamp must use UTC.", nameof(updatedAtUtc));
        }

        if (previousSnapshotPublishedAtUtc.HasValue &&
            previousSnapshotPublishedAtUtc.Value > updatedAtUtc)
        {
            throw new ArgumentException(
                "The previous snapshot cannot have been published after the pointer update.",
                nameof(previousSnapshotPublishedAtUtc));
        }

        this.ScopeKey = scopeKey;
        this.CurrentSnapshotId = currentSnapshotId;
        this.PreviousSnapshotId = previousSnapshotId;
        this.PreviousSnapshotPublishedAtUtc = previousSnapshotPublishedAtUtc;
        this.MethodologyVersion = methodologyVersion;
        this.SourceRevision = sourceRevision;
        this.HighestPublishedSourceRevision = highestPublishedSourceRevision;
        this.Version = version;
        this.UpdatedAtUtc = updatedAtUtc;
    }

    public RankingScopeKey ScopeKey { get; }

    public RankingSnapshotId CurrentSnapshotId { get; }

    public RankingSnapshotId? PreviousSnapshotId { get; }

    public DateTime? PreviousSnapshotPublishedAtUtc { get; }

    public RatingMethodologyVersion MethodologyVersion { get; }

    public long SourceRevision { get; }

    public long HighestPublishedSourceRevision { get; }

    public long Version { get; }

    public DateTime UpdatedAtUtc { get; }
}

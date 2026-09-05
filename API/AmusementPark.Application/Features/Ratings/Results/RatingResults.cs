using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingSummaryResult(
    RatingTargetType TargetType,
    string TargetId,
    long RatingCount,
    double AverageRating,
    double BayesianScore)
{
    private RatingMethodologyVersion? methodologyVersion;

    public int? Rank { get; init; }

    public DateTime? GeneratedAtUtc { get; init; }

    public long RatingObservationCount => this.Evidence?.RatingObservationCount ?? this.RatingCount;

    public long? UniqueContributorCount => this.Evidence?.UniqueContributorCount;

    public RankingEvidenceResult? Evidence { get; init; }

    public RatingMethodologyVersion? MethodologyVersion
    {
        get => this.methodologyVersion ?? this.Evidence?.MethodologyVersion;
        init => this.methodologyVersion = value;
    }
}

public sealed record RankingEvidenceResult(
    RankingEvidenceLevel Level,
    bool IsEligibleForMainRanking,
    long UniqueContributorCount,
    long RatingObservationCount,
    long? DirectParkContributorCount,
    long? ItemContributorCount,
    int? EligibleItemCount,
    int? EligibleCategoryCount,
    RatingMethodologyVersion MethodologyVersion,
    RankingIneligibilityReason? IneligibilityReason,
    int? NextThreshold);

public sealed record RatingDiagnosticsResult(
    DateTime GeneratedAtUtc,
    long ExecutionDurationMilliseconds,
    long TotalRatings,
    long DistinctNumericValueCount,
    IReadOnlyCollection<string> DistinctNumericValueSample,
    bool IsDistinctNumericValueSampleTruncated,
    RatingAnomalySummaryResult Anomalies,
    RatingAggregateIntegrityResult AggregateIntegrity,
    IReadOnlyCollection<RatingTargetDistributionResult> TargetDistribution,
    IReadOnlyCollection<RatingIndexStatusResult> Indexes);

public sealed record RatingAnomalySummaryResult(
    long NonNumericValueCount,
    long UnexpectedValueStorageTypeCount,
    long OutOfRangeValueCount,
    long NonHalfStepValueCount,
    long NearHalfStepValueCount,
    long MissingUserIdCount,
    long MissingTargetCount,
    long DuplicateVoteKeyCount,
    long ExtraDuplicateDocumentCount);

public sealed record RatingAggregateIntegrityResult(
    bool IsSourceComparisonEvaluated,
    bool IsOrphanCheckEvaluated,
    long SourceTargetCount,
    long MissingAggregateCount,
    long DivergentAggregateCount,
    long ContributorCountMismatchCount,
    long DerivedScoreMismatchCount,
    long OrphanAggregateCount);

public sealed record RatingTargetDistributionResult(
    string TargetType,
    string EvidenceBand,
    long TargetCount,
    long RatingObservationCount,
    long UniqueContributorCount);

public sealed record RatingIndexStatusResult(
    string Collection,
    string Name,
    bool IsPresent,
    bool IsUnique,
    bool IsHidden,
    bool HasUnexpectedOptions,
    bool SupportsExpectedQueries,
    bool MatchesExpectedDefinition,
    string ExpectedKeys,
    string? ActualKeys);

public sealed record RatingTargetMetadataResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    bool CanReceiveVisitorRatings);

public sealed record UserRatingResult(
    string Id,
    string UserId,
    RatingTargetType TargetType,
    string TargetId,
    string ParkId,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Value,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    RatingSummaryResult Summary);

public sealed record UserRatingListItemResult(
    string Id,
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Value,
    DateTime UpdatedAtUtc,
    RatingSummaryResult Summary);

public sealed record UserRatingStatBucketResult(
    string Key,
    string Label,
    long Count,
    double AverageRating);

public sealed record UserRatingStatsResult(
    long TotalRatings,
    double AverageRating,
    double HighestRating,
    double LowestRating,
    IReadOnlyCollection<UserRatingStatBucketResult> ByPark,
    IReadOnlyCollection<UserRatingStatBucketResult> ByTargetType,
    IReadOnlyCollection<UserRatingStatBucketResult> ByParkItemCategory);

public sealed record RatingRankingItemResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double RatingSum,
    double AverageRating,
    double BayesianScore)
{
    public long? UniqueContributorCount { get; init; }

    public bool? AggregateIntegrityIsValid { get; init; }
}

public sealed record RatingRankingSourceBatch(
    IReadOnlyCollection<RatingRankingItemResult> Sources,
    bool IsTruncated);

public sealed record RatingRankingParkCandidateBatch(
    IReadOnlyCollection<string> ParkIds,
    bool IsTruncated);

public sealed record ParkRatingRankingItemResult(
    string TargetId,
    string TargetName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double AverageRating,
    double BayesianScore);

public sealed record ParkRatingRankingCategoryResult(
    ParkItemCategory ParkItemCategory,
    long RatingCount,
    double AverageRating,
    double BayesianScore,
    IReadOnlyCollection<ParkRatingRankingItemResult> Items);

public sealed record ParkRatingRankingResult(
    int? Rank,
    string ParkId,
    string ParkName,
    long RatingCount,
    double Score,
    long ParkRatingCount,
    double ParkAverageRating,
    long ItemsRatingCount,
    double ItemsAverageRating,
    IReadOnlyCollection<ParkRatingRankingCategoryResult> Categories)
{
    public long RatingObservationCount => this.Evidence?.RatingObservationCount ?? this.RatingCount;

    public long? UniqueContributorCount => this.Evidence?.UniqueContributorCount;

    public RankingEvidenceResult? Evidence { get; init; }

    public RatingMethodologyVersion? MethodologyVersion => this.Evidence?.MethodologyVersion;

    public DateTime? GeneratedAtUtc { get; init; }
}

public sealed record ParkItemRatingRankingResult(
    int? Rank,
    string TargetId,
    string TargetName,
    string ParkId,
    string ParkName,
    ParkItemCategory ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double AverageRating,
    double BayesianScore)
{
    public long RatingObservationCount => this.Evidence?.RatingObservationCount ?? this.RatingCount;

    public long? UniqueContributorCount => this.Evidence?.UniqueContributorCount;

    public RankingEvidenceResult? Evidence { get; init; }

    public RatingMethodologyVersion? MethodologyVersion => this.Evidence?.MethodologyVersion;

    public DateTime? GeneratedAtUtc { get; init; }
}

public sealed record UserParkRatingRankingCategoryResult(
    ParkItemCategory ParkItemCategory,
    double AverageRating,
    IReadOnlyCollection<UserRatingListItemResult> Items);

public sealed record UserParkRatingRankingResult(
    int Rank,
    string ParkId,
    string ParkName,
    int RatingCount,
    double AverageRating,
    UserRatingListItemResult? ParkRating,
    IReadOnlyCollection<UserParkRatingRankingCategoryResult> Categories);

public sealed record UserParkItemRatingRankingResult(
    int Rank,
    UserRatingListItemResult Rating);

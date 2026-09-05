using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingScopeDiagnosticsResult(
    string ScopeKey,
    RankingTargetFamily TargetFamily,
    ParkItemCategory? ParkItemCategory,
    string MethodologyVersion,
    string? CurrentSnapshotId,
    DateTime? GeneratedAtUtc,
    DateTime? PublishedAtUtc,
    long? RebuildDurationMilliseconds,
    int TotalEntryCount,
    int EligibleEntryCount,
    long SourceRevision,
    long? PublishedSourceRevision,
    bool IsRebuildOutstanding,
    bool IsDiagnosticSourceTruncated,
    string? LastJobStatus,
    string? LastErrorCode,
    DateTime? LastJobUpdatedAtUtc);

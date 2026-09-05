using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record StartRankingSnapshotBuildRequest(
    RankingScopeKey ScopeKey,
    RatingMethodologyVersion MethodologyVersion,
    long SourceRevision,
    int TotalEntryCount,
    int EligibleEntryCount,
    RankingSnapshotChecksum Checksum,
    bool ForceRebuild = false);

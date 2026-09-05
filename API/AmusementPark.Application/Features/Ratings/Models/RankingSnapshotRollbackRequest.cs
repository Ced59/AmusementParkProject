using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotRollbackRequest(
    RankingScopeKey ScopeKey,
    RankingSnapshotId ExpectedCurrentSnapshotId,
    RankingSnapshotId ExpectedPreviousSnapshotId,
    long ExpectedPointerVersion);

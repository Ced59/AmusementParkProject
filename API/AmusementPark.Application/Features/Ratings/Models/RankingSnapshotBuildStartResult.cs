using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotBuildStartResult(
    RankingSnapshotBuildStartDisposition Disposition,
    RankingSnapshotHeader? Header);

using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotValidationResult(
    RankingSnapshotValidationDisposition Disposition,
    RankingSnapshotHeader? Header,
    string? ErrorCode = null);

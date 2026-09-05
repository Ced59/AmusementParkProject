using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingSnapshotBuildPlan(
    int TotalEntryCount,
    IReadOnlyCollection<RankingSnapshotEntry> EligibleEntries,
    bool IsSourceTruncated);

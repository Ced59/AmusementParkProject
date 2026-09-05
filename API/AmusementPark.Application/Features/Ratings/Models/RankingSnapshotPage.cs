using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotPage(
    RankingSnapshotHeader Header,
    IReadOnlyCollection<RankingSnapshotEntry> Entries,
    int Offset,
    int Limit);

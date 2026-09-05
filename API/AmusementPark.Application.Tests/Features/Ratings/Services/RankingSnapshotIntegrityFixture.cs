using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

internal sealed record RankingSnapshotIntegrityFixture(
    RankingSnapshotHeader Header,
    IReadOnlyCollection<RankingSnapshotChunk> Chunks);

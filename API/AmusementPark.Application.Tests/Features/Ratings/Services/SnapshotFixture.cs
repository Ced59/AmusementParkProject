using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Tests.Features.Ratings.Services;

internal sealed record SnapshotFixture(
    RankingSnapshotHeader Header,
    RankingSnapshotChunk Chunk,
    RankingPublicationPointer Pointer);

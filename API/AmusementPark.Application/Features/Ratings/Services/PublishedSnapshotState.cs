using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

internal sealed record PublishedSnapshotState(
    long SourceRevision,
    RankingPublicationPointer Pointer);

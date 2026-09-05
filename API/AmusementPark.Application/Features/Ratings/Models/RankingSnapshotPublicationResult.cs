using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotPublicationResult(
    RankingSnapshotPublicationDisposition Disposition,
    RankingPublicationPointer? Pointer);

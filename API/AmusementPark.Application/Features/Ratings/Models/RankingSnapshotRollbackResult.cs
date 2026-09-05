using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotRollbackResult(
    RankingSnapshotRollbackDisposition Disposition,
    RankingPublicationPointer? Pointer);

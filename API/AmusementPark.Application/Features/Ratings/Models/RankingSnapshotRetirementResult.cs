using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RankingSnapshotRetirementResult(
    RankingSnapshotRetirementDisposition Disposition,
    RankingPublicationPointer? Pointer);

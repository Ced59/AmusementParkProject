using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Services;

internal sealed record RatingRankingPreparedMutation(
    RatingTargetMetadataResult? Metadata,
    RatingRankingMutationPreparation Preparation,
    RatingRankingMutationRecoveryTarget RecoveryTarget,
    IReadOnlyCollection<ParkItemCategory> ProtectedCategories);

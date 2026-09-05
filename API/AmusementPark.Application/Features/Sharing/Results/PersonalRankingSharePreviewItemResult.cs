using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PersonalRankingSharePreviewItemResult(
    RatingTargetType TargetType,
    string TargetName,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Rating);

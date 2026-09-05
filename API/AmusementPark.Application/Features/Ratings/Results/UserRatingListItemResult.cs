using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRatingListItemResult(
    string Id,
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Value,
    DateTime UpdatedAtUtc,
    RatingSummaryResult Summary);

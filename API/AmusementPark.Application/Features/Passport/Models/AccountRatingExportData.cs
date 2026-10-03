using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountRatingExportData(
    RatingTargetType TargetType,
    string TargetName,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    double Value,
    DateTime UpdatedAtUtc);

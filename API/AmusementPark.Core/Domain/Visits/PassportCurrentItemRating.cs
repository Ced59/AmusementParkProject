using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportCurrentItemRating(
    string ParkItemId,
    RatingValue Rating);

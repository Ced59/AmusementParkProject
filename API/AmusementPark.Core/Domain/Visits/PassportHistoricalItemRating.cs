namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportHistoricalItemRating(
    string ParkItemId,
    long RatingCount,
    double Average);

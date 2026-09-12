using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportCurrentItemRatingObservation
{
    public PassportCurrentItemRatingObservation(string parkItemId, RatingValue rating)
    {
        this.ParkItemId = IdentifierRules.NormalizeRequired(parkItemId, nameof(parkItemId));
        this.Rating = rating;
    }

    public string ParkItemId { get; }

    public RatingValue Rating { get; }
}

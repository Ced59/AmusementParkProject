using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportItemRatingPoint(
    string RideOccurrenceId,
    string VisitId,
    VisitDate VisitDate,
    long SortPosition,
    RatingValue Rating);

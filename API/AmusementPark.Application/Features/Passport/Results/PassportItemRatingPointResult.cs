namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportItemRatingPointResult(
    string RideOccurrenceId,
    string VisitId,
    VisitDateResult Date,
    long SortPosition,
    double Rating);

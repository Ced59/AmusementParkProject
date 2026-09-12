using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportRatingTrendResult(
    PassportRatingTrendKind Kind,
    long FirstWindowRatingCount,
    long LastWindowRatingCount,
    double FirstWindowAverage,
    double LastWindowAverage,
    double Delta);

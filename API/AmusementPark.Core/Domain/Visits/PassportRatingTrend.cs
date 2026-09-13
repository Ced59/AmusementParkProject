namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportRatingTrend(
    PassportRatingTrendKind Kind,
    long FirstWindowRatingCount,
    long LastWindowRatingCount,
    double FirstWindowAverage,
    double LastWindowAverage,
    double Delta);

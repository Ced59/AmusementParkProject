namespace AmusementPark.Core.Domain.Visits;

public enum PassportRatingTrendKind
{
    Stable = 0,
    Rising = 1,
    Falling = 2,
}

public sealed record PassportRatingTrend(
    PassportRatingTrendKind Kind,
    long FirstWindowRatingCount,
    long LastWindowRatingCount,
    double FirstWindowAverage,
    double LastWindowAverage,
    double Delta);

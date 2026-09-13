namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRatingTrendDto
{
    public PassportRatingTrendKindDto Kind { get; init; }

    public long FirstWindowRatingCount { get; init; }

    public long LastWindowRatingCount { get; init; }

    public double FirstWindowAverage { get; init; }

    public double LastWindowAverage { get; init; }

    public double Delta { get; init; }
}

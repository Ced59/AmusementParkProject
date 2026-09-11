namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record YearRecapShareTrendResult(
    string Name,
    string Kind,
    long FirstWindowRatingCount,
    long LastWindowRatingCount,
    double FirstWindowAverage,
    double LastWindowAverage,
    double Delta);

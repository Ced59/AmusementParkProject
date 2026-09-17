namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripFitRecommendationSnapshotResult(
    string MethodVersion,
    string Explanation,
    DateTime CalculatedAtUtc);

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripNotificationCleanupBatchResult(
    int ScannedCount,
    int DeletedCount,
    string? NextCursor);

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record PassportProfileShareMissedItemResult(
    string Name,
    string Status,
    long? OccurrenceCount);

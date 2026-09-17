namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record UserNotificationCreationResult(
    long CreatedCount,
    long DuplicateCount);

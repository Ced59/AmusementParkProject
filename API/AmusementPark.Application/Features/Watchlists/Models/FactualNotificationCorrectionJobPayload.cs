namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record FactualNotificationCorrectionJobPayload(
    string EventId,
    long EventVersion,
    string? AfterNotificationId);

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record FactualNotificationDistributionReceipt(
    string EventId,
    DateTime CompletedAtUtc);

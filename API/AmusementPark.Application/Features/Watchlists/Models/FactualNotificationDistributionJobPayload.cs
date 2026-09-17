namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record FactualNotificationDistributionJobPayload(
    string EventId,
    string? AfterSubscriptionId);

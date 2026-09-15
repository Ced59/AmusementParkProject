namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record PublishedFactualEventCursor(
    DateTime PublishedAtUtc,
    string EventId);

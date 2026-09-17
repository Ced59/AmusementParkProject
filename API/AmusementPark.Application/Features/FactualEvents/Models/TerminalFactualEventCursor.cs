namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record TerminalFactualEventCursor(
    DateTime TerminalAtUtc,
    string EventId);

namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeOutboxCursor(
    DateTime RecordedAtUtc,
    string EntryId);

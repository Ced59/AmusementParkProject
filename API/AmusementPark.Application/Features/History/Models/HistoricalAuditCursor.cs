namespace AmusementPark.Application.Features.History.Models;

public sealed record HistoricalAuditCursor(
    DateTime OccurredAtUtc,
    int ResourceRevision);

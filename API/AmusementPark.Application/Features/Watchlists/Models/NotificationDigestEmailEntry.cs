using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record NotificationDigestEmailEntry(
    string? TargetName,
    string? ParentParkName,
    FactualEventType EventType,
    FactualChangeStatus Status,
    string? SourceLabel,
    DateTime VerifiedAtUtc);

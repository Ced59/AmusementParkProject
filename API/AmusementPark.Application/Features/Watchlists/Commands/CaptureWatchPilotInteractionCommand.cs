using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record CaptureWatchPilotInteractionCommand(
    string UserId,
    WatchPilotInteractionKind InteractionKind,
    string? NotificationId) : ICommand<ApplicationResult>;

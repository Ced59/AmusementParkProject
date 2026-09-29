using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record MutateLiveAlertNotificationCommand(
    string UserId,
    string NotificationId,
    long ExpectedVersion,
    LiveAlertNotificationMutation Mutation)
    : ICommand<ApplicationResult>;

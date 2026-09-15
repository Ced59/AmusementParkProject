using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record MarkUserNotificationReadCommand(
    string UserId,
    string NotificationId,
    long ExpectedVersion)
    : ICommand<ApplicationResult>;

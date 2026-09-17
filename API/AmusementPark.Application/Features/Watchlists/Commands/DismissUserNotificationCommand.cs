using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record DismissUserNotificationCommand(
    string UserId,
    string NotificationId,
    long ExpectedVersion)
    : ICommand<ApplicationResult>;

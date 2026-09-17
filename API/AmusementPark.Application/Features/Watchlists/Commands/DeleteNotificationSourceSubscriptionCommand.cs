using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record DeleteNotificationSourceSubscriptionCommand(
    string UserId,
    string NotificationId,
    long ExpectedSubscriptionVersion)
    : ICommand<ApplicationResult>;

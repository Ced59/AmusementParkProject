using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record MarkAllUserNotificationsReadCommand(string UserId)
    : ICommand<ApplicationResult>;

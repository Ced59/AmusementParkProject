using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record UnsubscribeNotificationEmailCommand(string Token)
    : ICommand<ApplicationResult>;

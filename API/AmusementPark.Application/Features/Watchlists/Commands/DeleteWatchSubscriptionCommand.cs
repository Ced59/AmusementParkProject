using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record DeleteWatchSubscriptionCommand(
    string UserId,
    string SubscriptionId,
    long ExpectedVersion)
    : ICommand<ApplicationResult>;

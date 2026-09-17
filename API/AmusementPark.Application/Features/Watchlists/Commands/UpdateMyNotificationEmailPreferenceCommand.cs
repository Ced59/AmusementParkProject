using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Commands;

public sealed record UpdateMyNotificationEmailPreferenceCommand(
    string UserId,
    NotificationEmailPreferenceInput Input)
    : ICommand<ApplicationResult<NotificationEmailPreferenceResult>>;

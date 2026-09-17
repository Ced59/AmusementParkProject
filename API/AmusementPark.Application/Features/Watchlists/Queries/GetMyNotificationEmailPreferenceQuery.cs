using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Queries;

public sealed record GetMyNotificationEmailPreferenceQuery(string UserId)
    : IQuery<ApplicationResult<NotificationEmailPreferenceResult>>;

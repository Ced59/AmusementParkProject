using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Queries;

public sealed record GetMyUserNotificationsQuery(
    string UserId,
    UserNotificationSearchCriteria Criteria)
    : IQuery<ApplicationResult<UserNotificationPageResult>>;

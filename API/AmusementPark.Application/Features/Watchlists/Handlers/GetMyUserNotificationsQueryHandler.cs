using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class GetMyUserNotificationsQueryHandler
    : IQueryHandler<GetMyUserNotificationsQuery, ApplicationResult<UserNotificationPageResult>>
{
    private readonly UserNotificationCenterService service;

    public GetMyUserNotificationsQueryHandler(UserNotificationCenterService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<UserNotificationPageResult>> HandleAsync(
        GetMyUserNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.SearchAsync(query.UserId, query.Criteria, cancellationToken);
    }
}

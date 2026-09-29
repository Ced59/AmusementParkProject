using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class GetMyLiveAlertsQueryHandler
    : IQueryHandler<GetMyLiveAlertsQuery, ApplicationResult<LiveAlertDashboardResult>>
{
    private readonly LiveAlertLifecycleService service;

    public GetMyLiveAlertsQueryHandler(LiveAlertLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<LiveAlertDashboardResult>> HandleAsync(
        GetMyLiveAlertsQuery query,
        CancellationToken cancellationToken = default)
    {
        return this.service.GetDashboardAsync(query.UserId, query.TargetId, cancellationToken);
    }
}

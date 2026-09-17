using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class ListMyWatchSubscriptionsQueryHandler
    : IQueryHandler<ListMyWatchSubscriptionsQuery,
        ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>>
{
    private readonly WatchSubscriptionLifecycleService service;

    public ListMyWatchSubscriptionsQueryHandler(WatchSubscriptionLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<IReadOnlyCollection<WatchSubscriptionResult>>> HandleAsync(
        ListMyWatchSubscriptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.ListAsync(
            query.UserId,
            query.TargetType,
            query.TargetId,
            cancellationToken);
    }
}

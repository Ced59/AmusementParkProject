using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class GetNotificationDeliveryMetricsQueryHandler
    : IQueryHandler<GetNotificationDeliveryMetricsQuery, NotificationDeliveryMetricsResult>
{
    private readonly INotificationDeliveryAttemptRepository repository;

    public GetNotificationDeliveryMetricsQueryHandler(
        INotificationDeliveryAttemptRepository repository)
    {
        this.repository = repository;
    }

    public Task<NotificationDeliveryMetricsResult> HandleAsync(
        GetNotificationDeliveryMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.FromUtc.Kind != DateTimeKind.Utc
            || query.ToUtc.Kind != DateTimeKind.Utc
            || query.ToUtc <= query.FromUtc
            || query.ToUtc - query.FromUtc > TimeSpan.FromDays(90))
        {
            throw new ArgumentException("The notification delivery metrics period is invalid.");
        }

        return this.repository.GetMetricsAsync(query.FromUtc, query.ToUtc, cancellationToken);
    }
}
